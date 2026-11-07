using Content.Server.Popups;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Bridges;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared._Stories.Heretic.Void;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Bridges;

public sealed partial class StoriesHereticBridgeSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private StoriesHereticVoidChillSystem _chill = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private StoriesHereticGraspSystem _grasp = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private StoriesHereticRuleSystem _rule = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private void InitializePrison()
    {
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticVoidPrisonEvent>(OnVoidPrison);
        SubscribeLocalEvent<StoriesHereticVoidPrisonComponent, EntityTerminatingEvent>(OnPrisonTerminating);
    }

    private void OnVoidPrison(Entity<StoriesHereticComponent> ent, ref StoriesHereticVoidPrisonEvent args)
    {
        var target = args.Target;
        if (args.Handled || target == ent.Owner || !IsValidVictim(target))
            return;

        if (HasComp<StoriesHereticShadowRealmComponent>(target)
            || _grasp.IsProtected(target)
            || _containers.IsEntityInContainer(target))
        {
            _popup.PopupEntity(Loc.GetString(args.Invalid), ent, ent, PopupType.SmallCaution);
            return;
        }

        var prison = Spawn(args.Prison, Transform(target).Coordinates);
        if (!TryComp<StoriesHereticVoidPrisonComponent>(prison, out var comp))
        {
            Del(prison);
            return;
        }

        var container = _containers.EnsureContainer<ContainerSlot>(prison, comp.ContainerId);
        if (!_containers.Insert(target, container))
        {
            Del(prison);
            return;
        }

        args.Handled = true;

        comp.Prisoner = target;
        comp.ActiveTime = _timing.CurTime + comp.OpeningTime;
        comp.EndTime = _timing.CurTime + comp.Duration;
        EnsureComp<StoriesHereticVoidPrisonerComponent>(target);
        _appearance.SetData(prison, StoriesHereticVoidPrisonVisuals.State, comp.State);
    }

    private void OnPrisonTerminating(Entity<StoriesHereticVoidPrisonComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Prisoner is not { } target)
            return;

        ent.Comp.Prisoner = null;
        RemComp<StoriesHereticVoidPrisonerComponent>(target);
        if (TerminatingOrDeleted(Transform(ent).ParentUid)
            || !_containers.TryGetContainer(ent, ent.Comp.ContainerId, out var container)
            || !container.Contains(target))
            return;

        var coords = Transform(ent).Coordinates;
        _containers.Remove(target, container, destination: coords);

        if (!IsTileFree(coords) && _rule.TryGetRandomStationCoordinates(out var safe))
        {
            _transform.SetCoordinates(target, safe);
            _transform.AttachToGridOrMap(target);
        }

        _chill.ApplyChill(target, ent.Comp.ChillStacks);
    }

    private void ReleasePrisoner(Entity<StoriesHereticVoidPrisonComponent> ent)
    {
        if (ent.Comp.Prisoner is not { } target)
            return;

        ent.Comp.Prisoner = null;
        if (_containers.TryGetContainer(ent, ent.Comp.ContainerId, out var container) && container.Contains(target))
            _containers.Remove(target, container, destination: Transform(ent).Coordinates);

        RemComp<StoriesHereticVoidPrisonerComponent>(target);
        _chill.ApplyChill(target, ent.Comp.ChillStacks);
    }

    private bool IsTileFree(EntityCoordinates coords)
    {
        if (_transform.GetGrid(coords) is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = _map.CoordinatesToTile(gridUid, grid, coords);
        return _map.TryGetTileRef(gridUid, grid, tile, out var tileRef)
               && !tileRef.Tile.IsEmpty
               && !_turf.IsSpace(tileRef)
               && !_turf.IsTileBlocked(gridUid, tile, CollisionGroup.Impassable, grid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticVoidPrisonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            switch (comp.State)
            {
                case StoriesHereticVoidPrisonState.Opening when now >= comp.ActiveTime:
                    comp.State = StoriesHereticVoidPrisonState.Active;
                    _appearance.SetData(uid, StoriesHereticVoidPrisonVisuals.State, comp.State);
                    break;
                case StoriesHereticVoidPrisonState.Active when now >= comp.EndTime:
                    comp.State = StoriesHereticVoidPrisonState.Closing;
                    comp.EndTime = now + comp.ClosingTime;
                    _appearance.SetData(uid, StoriesHereticVoidPrisonVisuals.State, comp.State);
                    ReleasePrisoner((uid, comp));
                    break;
                case StoriesHereticVoidPrisonState.Closing when now >= comp.EndTime:
                    QueueDel(uid);
                    break;
            }
        }
    }
}
