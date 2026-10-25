using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Atmos;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Sprite;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedScaleVisualsSystem _scale = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private readonly List<EntityUid> _anchored = new();
    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();

    private void InitializeBlast()
    {
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticVoidBlastEvent>(OnVoidBlast);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticVoidIceComponent>();
        while (query.MoveNext(out var uid, out var ice))
        {
            if (now >= ice.EndTime)
                QueueDel(uid);
        }

        UpdateStorms(now);
    }

    private void OnVoidBlast(Entity<StoriesHereticComponent> ent, ref StoriesHereticVoidBlastEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var origin = _transform.GetMapCoordinates(ent);
        var facing = _transform.GetWorldRotation(ent).ToWorldVec();
        var halfAngle = (float) Angle.FromDegrees(args.HalfAngleDegrees).Theta;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, args.Range, _mobs);
        foreach (var mob in _mobs)
        {
            if (mob.Owner == ent.Owner
                || HasComp<StoriesHereticComponent>(mob)
                || _heretic.IsServant(mob)
                || !_mobState.IsAlive(mob))
                continue;

            if (!StoriesHereticGeometry.IsInCone(origin.Position, facing, _transform.GetMapCoordinates(mob).Position, halfAngle))
                continue;

            _damageable.TryChangeDamage(mob.Owner, args.Damage, origin: ent);
            _chill.ApplyChill(mob, args.ChillStacks);
        }

        SpawnIce(ent, args);

        for (var i = 0; i < args.Frost.Count; i++)
        {
            var angle = new Angle(_random.NextFloat(-halfAngle, halfAngle));
            var distance = _random.NextFloat(0.5f, args.Range);
            SpawnFrost(args.Frost, origin.Offset(angle.RotateVec(facing) * distance));
        }
    }

    private void SpawnIce(EntityUid heretic, StoriesHereticVoidBlastEvent args)
    {
        var xform = Transform(heretic);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var forward = (_transform.GetWorldRotation(heretic) - _transform.GetWorldRotation(gridUid)).ToAtmosDirectionCardinal().CardinalToIntVec();
        var side = new Vector2i(-forward.Y, forward.X);
        var start = _map.CoordinatesToTile(gridUid, grid, xform.Coordinates);

        for (var row = 1; row <= args.IceRows; row++)
        {
            for (var lateral = -(row - 1); lateral <= row - 1; lateral++)
            {
                var tile = start + forward * row + side * lateral;
                if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef)
                    || tileRef.Tile.IsEmpty
                    || _turf.IsTileBlocked(gridUid, tile, CollisionGroup.Impassable, grid)
                    || HasIce(gridUid, grid, tile))
                    continue;

                var ice = Spawn(args.Ice, _map.GridTileToLocal(gridUid, grid, tile));
                SetIceEndTime(ice);
            }
        }
    }

    private bool HasIce(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        _anchored.Clear();
        _map.GetAnchoredEntities((gridUid, grid), tile, _anchored);
        foreach (var uid in _anchored)
        {
            if (HasComp<StoriesHereticVoidIceComponent>(uid))
                return true;
        }

        return false;
    }

    private void SetIceEndTime(EntityUid uid)
    {
        if (!TryComp<StoriesHereticVoidIceComponent>(uid, out var ice))
            return;

        var warm = _atmosphere.GetTileMixture((uid, Transform(uid)))?.Temperature > ice.WarmThreshold;
        ice.EndTime = _timing.CurTime + (warm ? ice.WarmMeltTime : ice.ColdMeltTime);
    }

    private void SpawnFrost(StoriesHereticFlameEffects frost, MapCoordinates coords)
    {
        var rotation = new Angle(_random.NextFloat((float) -frost.MaxRotation.Theta, (float) frost.MaxRotation.Theta));
        var particle = Spawn(frost.Effect, coords, rotation: rotation);
        _scale.SetSpriteScale(particle, new Vector2(_random.NextFloat(frost.MinScale, frost.MaxScale)));

        if (TryComp<TimedDespawnComponent>(particle, out var despawn))
            despawn.Lifetime = _random.NextFloat(frost.MinLifetime, frost.MaxLifetime);
    }
}
