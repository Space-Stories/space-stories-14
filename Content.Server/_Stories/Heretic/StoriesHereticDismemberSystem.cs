using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic;

/// <summary>
/// Расчленение трупа клинком еретика: глаголы на мёртвом гуманоиде, отделяющие голову, руки и ноги.
/// </summary>
public sealed partial class StoriesHereticDismemberSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private DetachableOrganSystem _detachable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<OrganCategoryPrototype>[] Categories = ["Head", "ArmLeft", "ArmRight", "LegLeft", "LegRight"];
    private static readonly TimeSpan DismemberTime = TimeSpan.FromSeconds(5);
    private static readonly SoundSpecifier DismemberSound = new SoundPathSpecifier("/Audio/Effects/gib1.ogg");

    private VerbCategory? _category;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<BodyComponent, StoriesHereticDismemberDoAfterEvent>(OnDoAfter);
    }

    private void OnGetVerbs(Entity<BodyComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess
            || !CanDismember(args.User, ent)
            || !_containers.TryGetContainer(ent, BodyComponent.ContainerID, out var container))
            return;

        _category ??= new VerbCategory("stories-heretic-dismember-category", null);

        var user = args.User;
        foreach (var organ in container.ContainedEntities)
        {
            if (!IsDismemberable(organ))
                continue;

            var part = organ;
            var name = PartName(part);
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("stories-heretic-dismember-verb", ("part", name)),
                Category = _category,
                Act = () => TryDismember(user, ent, part),
            });
        }
    }

    private bool CanDismember(EntityUid user, EntityUid corpse)
    {
        return HasComp<StoriesHereticComponent>(user)
               && HasBlade(user)
               && HasComp<HumanoidProfileComponent>(corpse)
               && _mobState.IsDead(corpse)
               && !HasComp<StoriesHereticShadowRealmComponent>(corpse);
    }

    private string PartName(EntityUid organ)
    {
        return Loc.GetString($"stories-heretic-dismember-part-{Comp<OrganComponent>(organ).Category}");
    }

    private bool HasBlade(EntityUid user)
    {
        return _hands.GetActiveItem(user) is { } item && HasComp<StoriesHereticBladeComponent>(item);
    }

    private bool IsDismemberable(EntityUid organ)
    {
        return HasComp<DetachableOrganComponent>(organ)
               && TryComp<OrganComponent>(organ, out var comp)
               && comp.Category is { } cat
               && Array.IndexOf(Categories, cat) >= 0;
    }

    private bool IsInBody(EntityUid corpse, EntityUid organ)
    {
        return _containers.TryGetContainer(corpse, BodyComponent.ContainerID, out var container)
               && container.Contains(organ);
    }

    public bool TryDismember(EntityUid user, EntityUid corpse, EntityUid organ)
    {
        if (!CanDismember(user, corpse) || !IsDismemberable(organ) || !IsInBody(corpse, organ))
            return false;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            DismemberTime,
            new StoriesHereticDismemberDoAfterEvent(),
            corpse,
            target: corpse,
            used: organ)
        {
            BreakOnMove = true,
            DistanceThreshold = 1.5f,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent,
        });
    }

    private void OnDoAfter(Entity<BodyComponent> ent, ref StoriesHereticDismemberDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } organ)
            return;

        args.Handled = true;
        if (!CanDismember(args.User, ent))
            return;

        var name = PartName(organ);
        if (Dismember(ent, organ) == null)
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):user} dismembered {ToPrettyString(organ):entity} from {ToPrettyString(ent):target}");
        _audio.PlayPvs(DismemberSound, ent);
        _popup.PopupEntity(Loc.GetString("stories-heretic-dismember-done",
            ("user", args.User), ("part", name), ("corpse", ent.Owner)), ent, PopupType.MediumCaution);
    }

    /// <summary>Мгновенно отделяет орган от трупа. Живому телу ничего не делает.</summary>
    public EntityUid? Dismember(EntityUid corpse, EntityUid organ)
    {
        if (!_mobState.IsDead(corpse) || HasComp<StoriesHereticShadowRealmComponent>(corpse)
            || !IsDismemberable(organ) || !IsInBody(corpse, organ))
            return null;

        return _detachable.Detach(organ);
    }
}
