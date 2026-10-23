using System.Numerics;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidSystem
{
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;

    private void InitializeSeeking()
    {
        SubscribeLocalEvent<StoriesHereticBladeComponent, AfterInteractEvent>(OnBladeAfterInteract);
    }

    private void OnBladeAfterInteract(Entity<StoriesHereticBladeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || args.CanReach
            || args.Target is not { } target
            || !TryComp<StoriesHereticSeekingBladeComponent>(args.User, out var seeking)
            || !TryComp<StoriesHereticComponent>(args.User, out var heretic)
            || heretic.Path != VoidPath
            || !TryComp<StoriesHereticMarkComponent>(target, out var mark)
            || mark.Path != VoidPath
            || !TryComp<MeleeWeaponComponent>(ent, out var weapon)
            || _timing.CurTime < seeking.NextUse
            || weapon.NextAttack > _timing.CurTime)
            return;

        var user = args.User;
        var userPos = _transform.GetMapCoordinates(user);
        var targetPos = _transform.GetMapCoordinates(target);
        if (userPos.MapId != targetPos.MapId
            || (targetPos.Position - userPos.Position).Length() > seeking.Range
            || !_examine.InRangeUnOccluded(user, target, seeking.Range + 1f))
            return;

        var direction = Vector2.Normalize(targetPos.Position - userPos.Position);
        if (!TryFindBehind(target, direction, out var destination))
            return;

        args.Handled = true;

        StopPulls(user);
        _transform.SetCoordinates(user, destination);
        _transform.AttachToGridOrMap(user);
        _combatMode.SetInCombatMode(user, true);
        if (_melee.AttemptLightAttack(user, ent, weapon, target))
            seeking.NextUse = _timing.CurTime + seeking.Cooldown;
    }

    private bool TryFindBehind(EntityUid target, Vector2 direction, out EntityCoordinates destination)
    {
        destination = default;
        var targetCoords = Transform(target).Coordinates;
        var side = new Vector2(-direction.Y, direction.X);
        foreach (var offset in new[] { direction, direction + side, direction - side })
        {
            var coords = SnapToTile(targetCoords.Offset(offset));
            if (!IsTileFree(coords))
                continue;

            destination = coords;
            return true;
        }

        return false;
    }

    private EntityCoordinates SnapToTile(EntityCoordinates coords)
    {
        if (_transform.GetGrid(coords) is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return coords;

        return _map.GridTileToLocal(gridUid, grid, _map.CoordinatesToTile(gridUid, grid, coords));
    }
}
