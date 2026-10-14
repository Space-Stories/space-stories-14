using System.Linq;
using System.Numerics;
using Content.Server._Stories.Heretic.Summons;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.DoAfter;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

// Сырой пророк: Связь Мансуса (частный канал связи) и Расширение взора (зум камеры).
public sealed partial class StoriesHereticFleshSystem
{
    [Dependency] private SharedContentEyeSystem _contentEye = default!;
    [Dependency] private StoriesHereticGraspSystem _grasp = default!;
    [Dependency] private StoriesHereticSummonSystem _summons = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    private static readonly ProtoId<RadioChannelPrototype> MansusChannel = "STHereticMansusLink";

    private void InitializeLink()
    {
        SubscribeLocalEvent<StoriesHereticRawProphetComponent, MindAddedMessage>(OnProphetMindAdded);
        SubscribeLocalEvent<StoriesHereticRawProphetComponent, MobStateChangedEvent>(OnProphetMobState);
        SubscribeLocalEvent<StoriesHereticRawProphetComponent, ComponentShutdown>(OnProphetShutdown);
        SubscribeLocalEvent<StoriesHereticRawProphetComponent, StoriesHereticMansusLinkEvent>(OnMansusLink);
        SubscribeLocalEvent<StoriesHereticRawProphetComponent, StoriesHereticMansusLinkDoAfterEvent>(OnMansusLinkDoAfter);
        SubscribeLocalEvent<StoriesHereticRawProphetComponent, StoriesHereticExpandSightEvent>(OnExpandSight);
    }

    private void GrantMansus(EntityUid uid)
    {
        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        EnsureComp<IntrinsicRadioTransmitterComponent>(uid).Channels.Add(MansusChannel);
        EnsureComp<ActiveRadioComponent>(uid).Channels.Add(MansusChannel);
    }

    // Снимаем только канал Мансуса: остальные каналы цели остаются.
    private void RevokeMansus(EntityUid uid)
    {
        if (TryComp<IntrinsicRadioTransmitterComponent>(uid, out var transmitter))
            transmitter.Channels.Remove(MansusChannel);
        if (TryComp<ActiveRadioComponent>(uid, out var active))
            active.Channels.Remove(MansusChannel);
    }

    private void OnProphetMindAdded(Entity<StoriesHereticRawProphetComponent> ent, ref MindAddedMessage args)
    {
        if (_summons.GetMaster(ent) is not { } master || TerminatingOrDeleted(master))
            return;

        ent.Comp.Master = master;
        GrantMansus(master);
    }

    private void OnProphetMobState(Entity<StoriesHereticRawProphetComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            BreakLinks(ent);
    }

    private void OnProphetShutdown(Entity<StoriesHereticRawProphetComponent> ent, ref ComponentShutdown args)
    {
        BreakLinks(ent);
    }

    private void BreakLinks(Entity<StoriesHereticRawProphetComponent> ent)
    {
        if (ent.Comp.Master is { } master && !TerminatingOrDeleted(master))
            RevokeMansus(master);
        ent.Comp.Master = null;

        var linked = ent.Comp.Linked.ToArray();
        ent.Comp.Linked.Clear();
        foreach (var target in linked)
        {
            if (TerminatingOrDeleted(target))
                continue;

            RevokeMansus(target);
            RemComp<StoriesHereticMansusLinkComponent>(target);
            _chat.TryEmoteWithChat(target, ScreamEmote);
            _stun.TryAddParalyzeDuration(target, ent.Comp.BreakParalyze);
        }
    }

    private bool CanLink(Entity<StoriesHereticRawProphetComponent> ent, EntityUid target)
    {
        if (target == ent.Owner || !HasComp<MobStateComponent>(target) || HasComp<StoriesHereticMansusLinkComponent>(target))
            return false;

        if (HasComp<MindShieldComponent>(target) || _grasp.IsProtected(target))
        {
            _popup.PopupEntity(Loc.GetString("stories-heretic-mansus-link-blocked"), target, ent, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void OnMansusLink(Entity<StoriesHereticRawProphetComponent> ent, ref StoriesHereticMansusLinkEvent args)
    {
        if (args.Handled || !CanLink(ent, args.Target))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, args.Duration,
            new StoriesHereticMansusLinkDoAfterEvent(), ent, target: args.Target)
        {
            BreakOnMove = true,
            DistanceThreshold = args.Range,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent,
        });
    }

    private void OnMansusLinkDoAfter(Entity<StoriesHereticRawProphetComponent> ent, ref StoriesHereticMansusLinkDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target || !CanLink(ent, target))
            return;

        args.Handled = true;
        GrantMansus(target);
        EnsureComp<StoriesHereticMansusLinkComponent>(target).Prophet = ent;
        ent.Comp.Linked.Add(target);
        _popup.PopupEntity(Loc.GetString("stories-heretic-mansus-link-linked"), target, target, PopupType.MediumCaution);
    }

    private void OnExpandSight(Entity<StoriesHereticRawProphetComponent> ent, ref StoriesHereticExpandSightEvent args)
    {
        if (args.Handled || !HasComp<ContentEyeComponent>(ent))
            return;

        args.Handled = true;
        ent.Comp.SightExpanded = !ent.Comp.SightExpanded;
        var zoom = Vector2.One * (ent.Comp.SightExpanded ? args.Zoom : 1f);
        _contentEye.SetMaxZoom(ent, zoom);
        _contentEye.SetZoom(ent, zoom);
    }
}
