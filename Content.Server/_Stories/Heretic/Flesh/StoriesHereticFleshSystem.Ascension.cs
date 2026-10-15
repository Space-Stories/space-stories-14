using Content.Server.Polymorph.Components;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Flesh;

// Последний гимн жреца: Вознесение Плоти и Владыка Ночи.
public sealed partial class StoriesHereticFleshSystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;

    private readonly HashSet<Entity<HumanoidProfileComponent>> _witnesses = new();

    private void InitializeAscension()
    {
        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticShedFormEvent>(OnShedForm);
        SubscribeLocalEvent<StoriesHereticArmEaterComponent, UserActivateInWorldEvent>(OnArmEaterActivate);
    }

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        if (args.Effect != StoriesHereticRitualEffect.Ascension
            || args.Heretic.Comp.Path != FleshPath
            || !ProtoMan.TryIndex(args.Ritual, out var proto)
            || !proto.TryComp<StoriesHereticFleshAscensionRitualComponent>(out var ritual, _factory))
            return;

        if (args.Heretic.Comp.Ascended || !args.Heretic.Comp.CanAscend)
        {
            args.FailReason = Loc.GetString(args.Heretic.Comp.Ascended
                ? "stories-heretic-ascension-done"
                : "stories-heretic-learn-ascension-locked");
            return;
        }

        var corpses = new List<EntityUid>();
        foreach (var uid in args.Nearby)
        {
            if (corpses.Count >= ritual.Corpses)
                break;

            if (HasComp<HumanoidProfileComponent>(uid) && _mobState.IsDead(uid))
                corpses.Add(uid);
        }

        if (corpses.Count < ritual.Corpses)
        {
            if (corpses.Count > 0)
                args.FailReason = Loc.GetString("stories-heretic-flesh-ascension-corpses", ("count", ritual.Corpses - corpses.Count));
            return;
        }

        foreach (var corpse in corpses)
        {
            _heretic.DropEverything(corpse);
        }

        args.Consumed.AddRange(corpses);
        _heretic.SetAscended(args.Heretic);
        foreach (var action in ritual.Actions)
        {
            _actions.AddAction(args.Heretic, action);
        }

        _chat.DispatchGlobalAnnouncement(Loc.GetString(ritual.Announcement, ("name", Name(args.Heretic))),
            colorOverride: ritual.AnnouncementColor);
        _audio.PlayGlobal(ritual.Sound, Filter.Broadcast(), true);
        _heretic.RemoveRitual(args.Heretic, args.Ritual);
        args.Success = true;
    }

    private void OnShedForm(Entity<StoriesHereticComponent> ent, ref StoriesHereticShedFormEvent args)
    {
        if (args.Handled)
            return;

        // Кричим до превращения: после него тело уже в хранилище полиморфа, и крик никто не услышит.
        // Поэтому сначала те же проверки, что делает PolymorphEntity: без превращения крика нет.
        var config = ProtoMan.Index(args.Polymorph).Configuration;
        if (!CanPolymorph(ent, config))
            return;

        _chat.TrySendInGameICMessage(ent, Loc.GetString(args.Speech), InGameICChatType.Speak, false);

        // Handled (перезарядка) — только если превращение удалось.
        if (_polymorph.PolymorphEntity(ent, config) is not { } lord)
            return;

        args.Handled = true;

        // Ужас Владыки (замена мозговых травм tg): каждый живой видящий не-еретик и не-слуга с шансом.
        _witnesses.Clear();
        _lookup.GetEntitiesInRange(Transform(lord).Coordinates, args.FearRange, _witnesses);
        foreach (var witness in _witnesses)
        {
            if (HasComp<StoriesHereticComponent>(witness)
                || _heretic.IsServant(witness)
                || !_mobState.IsAlive(witness)
                || !_examine.InRangeUnOccluded(lord, witness, args.FearRange)
                || !_random.Prob(args.FearChance))
                continue;

            _jitter.DoJitter(witness, args.JitterTime, true);
            _statusEffects.TryUpdateStatusEffectDuration(witness, args.StutterEffect, args.StutterTime);
            _statusEffects.TryUpdateStatusEffectDuration(witness, args.DizzyEffect, args.DizzyTime);
            _popup.PopupEntity(Loc.GetString(args.FearMessage), witness, witness, PopupType.LargeCaution);
        }
    }

    /// <summary>
    /// Условия отказа <c>PolymorphSystem.PolymorphEntity</c>
    /// до спавна новой формы: повторное превращение и перезарядка полиморфа.
    /// </summary>
    private bool CanPolymorph(EntityUid uid, PolymorphConfiguration config)
    {
        if (!config.IgnoreAllowRepeatedMorphs
            && TryComp<PolymorphedEntityComponent>(uid, out var current)
            && !current.Configuration.AllowRepeatedMorphs)
            return false;

        return !TryComp<PolymorphableComponent>(uid, out var polymorphable)
               || polymorphable.LastPolymorphEnd is not { } end
               || _timing.CurTime >= end + config.Cooldown;
    }

    private void OnArmEaterActivate(Entity<StoriesHereticArmEaterComponent> ent, ref UserActivateInWorldEvent args)
    {
        if (args.Handled || !IsDetachedArm(args.Target, ent.Comp))
            return;

        args.Handled = true;
        // Лечит долю максимума по всем имеющимся видам урона, пропорционально их величине.
        if (_thresholds.TryGetDeadThreshold(ent, out var max))
            _damageable.HealDistributed(ent.Owner, -(max.Value * ent.Comp.HealFraction), origin: ent);

        _audio.PlayPvs(ent.Comp.Sound, ent);
        QueueDel(args.Target);
    }

    // Отделённая часть тела (DetachedBody) — без MobState, органы лежат в её контейнере.
    private bool IsDetachedArm(EntityUid uid, StoriesHereticArmEaterComponent eater)
    {
        if (HasComp<MobStateComponent>(uid) || !_containers.TryGetContainer(uid, BodyComponent.ContainerID, out var organs))
            return false;

        foreach (var organ in organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organ, out var comp) && comp.Category is { } category && eater.Categories.Contains(category))
                return true;
        }

        return false;
    }
}
