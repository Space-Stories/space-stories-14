using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

// «Сшить плоть»: лечение слуг и извлечение органов глаголами.
public sealed partial class StoriesHereticFleshSystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private OrganRelationSystem _relations = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly ProtoId<EmotePrototype> ScreamEmote = "Scream";
    private static readonly EntProtoId SlurredEffect = "StatusEffectSlurred";
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    private static readonly ProtoId<OrganCategoryPrototype> LungsCategory = "Lungs";

    private static readonly ProtoId<OrganCategoryPrototype>[] KnitCategories =
        ["Eyes", "Tongue", "Heart", "Lungs", "Liver", "Stomach", "Appendix", "Kidneys", "Ears"];

    private VerbCategory? _knitCategory;

    private void InitializeKnit()
    {
        SubscribeLocalEvent<StoriesHereticKnitFleshComponent, StoriesHereticKnitFleshEvent>(OnKnitFlesh);
        SubscribeLocalEvent<StoriesHereticKnitFleshComponent, StoriesHereticKnitHealDoAfterEvent>(OnKnitHealDoAfter);
        SubscribeLocalEvent<BodyComponent, StoriesHereticKnitExtractDoAfterEvent>(OnKnitDoAfter);
        SubscribeLocalEvent<HumanoidProfileComponent, GetVerbsEvent<AlternativeVerb>>(OnGetExtractVerbs);
    }

    private void OnKnitFlesh(Entity<StoriesHereticKnitFleshComponent> ent, ref StoriesHereticKnitFleshEvent args)
    {
        if (args.Handled || !_heretic.IsServant(args.Target, ent) || !_mobState.IsAlive(args.Target))
            return;

        // Перезарядка (Handled) тратится, только если лечение началось.
        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.HealTime,
            new StoriesHereticKnitHealDoAfterEvent(),
            ent,
            target: args.Target)
        {
            BreakOnMove = true,
            DistanceThreshold = ent.Comp.DistanceThreshold,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent,
        }))
            return;

        args.Handled = true;
        _chat.TrySendInGameICMessage(ent, Loc.GetString(ent.Comp.HealSpeech), InGameICChatType.Whisper, false);
    }

    private void OnKnitHealDoAfter(Entity<StoriesHereticKnitFleshComponent> ent, ref StoriesHereticKnitHealDoAfterEvent args)
    {
        // За секунду слуга мог умереть или смениться хозяин.
        if (args.Cancelled || args.Handled || args.Target is not { } target
            || !_heretic.IsServant(target, ent) || !_mobState.IsAlive(target))
            return;

        args.Handled = true;
        // HealDistributed напрямую, а не через эффект: эффект теряет origin (кто лечил).
        foreach (var (group, amount) in ent.Comp.Heal.Damage)
        {
            _damageable.HealDistributed(target, amount, group, ent);
        }

        _audio.PlayPvs(ent.Comp.HealSound, target);
    }

    /// <summary>Глаголы «Извлечь: орган». Подписка на гуманоида: тело уже занято расчленением.</summary>
    private void OnGetExtractVerbs(Entity<HumanoidProfileComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess
            || !TryComp<StoriesHereticKnitFleshComponent>(args.User, out var knit)
            || !_containers.TryGetContainer(ent, BodyComponent.ContainerID, out var container))
            return;

        _knitCategory ??= new VerbCategory("stories-heretic-knit-category", null);

        var user = args.User;
        foreach (var organ in container.ContainedEntities)
        {
            var part = organ;
            if (!CanExtract((user, knit), ent, part))
                continue;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("stories-heretic-knit-verb", ("part", PartName(part))),
                Category = _knitCategory,
                Act = () => TryStartExtract(user, ent, part),
            });
        }
    }

    private string PartName(EntityUid organ)
    {
        return Loc.GetString($"stories-heretic-knit-part-{Comp<OrganComponent>(organ).Category}");
    }

    private bool CanExtract(Entity<StoriesHereticKnitFleshComponent> user, EntityUid target, EntityUid organ)
    {
        if (!HasComp<HumanoidProfileComponent>(target)
            || HasComp<StoriesHereticShadowRealmComponent>(target)
            || !_heretic.HasFocus(user)
            || !TryComp<OrganComponent>(organ, out var comp)
            || comp.Category is not { } cat
            || Array.IndexOf(KnitCategories, cat) < 0
            || !_containers.TryGetContainer(target, BodyComponent.ContainerID, out var container)
            || !container.Contains(organ))
            return false;

        // Из живых нельзя достать сердце и лёгкие.
        return _mobState.IsDead(target) || (cat != HeartCategory && cat != LungsCategory);
    }

    public bool TryStartExtract(EntityUid user, EntityUid target, EntityUid organ)
    {
        if (!TryComp<StoriesHereticKnitFleshComponent>(user, out var knit) || !CanExtract((user, knit), target, organ))
            return false;

        var living = !_mobState.IsDead(target);
        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            living ? knit.LivingTime : knit.CorpseTime,
            new StoriesHereticKnitExtractDoAfterEvent(),
            target,
            target: target,
            used: organ)
        {
            BreakOnMove = true,
            DistanceThreshold = knit.DistanceThreshold,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent,
        }))
            return false;

        if (living)
        {
            _chat.TryEmoteWithChat(target, ScreamEmote);
            _statusEffects.TryUpdateStatusEffectDuration(target, SlurredEffect, knit.LivingTime);
        }

        return true;
    }

    private void OnKnitDoAfter(Entity<BodyComponent> ent, ref StoriesHereticKnitExtractDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } organ)
            return;

        args.Handled = true;
        var name = PartName(organ);
        if (!Extract(args.User, ent, organ))
            return;

        _popup.PopupEntity(Loc.GetString("stories-heretic-knit-done",
            ("user", args.User), ("part", name), ("target", ent.Owner)), ent, PopupType.MediumCaution);
    }

    /// <summary>Мгновенно достаёт орган из тела и кладёт его еретику в руку, если она свободна.</summary>
    public bool Extract(EntityUid user, EntityUid target, EntityUid organ)
    {
        if (!TryComp<StoriesHereticKnitFleshComponent>(user, out var knit) || !CanExtract((user, knit), target, organ)
            || !_containers.TryGetContainer(target, BodyComponent.ContainerID, out var container))
            return false;

        var dropAt = Transform(target).Coordinates;
        if (!_containers.Remove(organ, container, destination: dropAt))
            return false;

        // Связь с родителем рвём только после удачного извлечения: иначе орган остался бы в теле без родителя.
        _relations.Orphan(organ);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):user} extracted {ToPrettyString(organ):entity} from {ToPrettyString(target):target}");
        _audio.PlayPvs(knit.ExtractSound, target);

        // Испорченный орган мог взорваться при извлечении.
        if (!TerminatingOrDeleted(organ))
            _hands.TryPickupAnyHand(user, organ);

        return true;
    }
}
