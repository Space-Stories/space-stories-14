using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticSacrificeSystem
{
    [Dependency] private ITileDefinitionManager _tileDefs = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;

    private const string RealmTile = "FloorDark";
    private const int PlatformRadius = 4;
    private const int PlatformSpacing = 12;
    private static readonly Color RealmLight = Color.FromHex("#1b0f24");

    private Entity<MapGridComponent>? _realmGrid;
    private bool _realmFromFile;
    private readonly List<EntityCoordinates> _realmSpawns = new();
    private int _nextPlatform;

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _realmGrid = null;
        _realmFromFile = false;
        _nextPlatform = 0;
    }

    private void SendToShadowRealm(EntityUid victim, EntityCoordinates returnCoords)
    {
        if (!TryEnsureRealm(out var grid))
            return;

        EntityCoordinates destination;
        if (_realmFromFile)
        {
            destination = PickRealmSpawn(grid.Value);
        }
        else
        {
            var center = new Vector2i(_nextPlatform++ * PlatformSpacing, 0);
            BuildPlatform(grid.Value, center);
            destination = new EntityCoordinates(grid.Value, new Vector2(center.X + 0.5f, center.Y + 0.5f));
        }

        var realm = EnsureComp<StoriesHereticShadowRealmComponent>(victim);
        realm.ReturnTime = _timing.CurTime + realm.Duration;
        realm.NextDamage = _timing.CurTime + realm.DamageInterval;
        realm.ReturnCoordinates = returnCoords;

        _transform.SetCoordinates(victim, destination);
        _transform.AttachToGridOrMap(victim);
    }

    private EntityCoordinates PickRealmSpawn(EntityUid grid)
    {
        _realmSpawns.Clear();
        var query = EntityQueryEnumerator<StoriesHereticShadowRealmSpawnComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == grid)
                _realmSpawns.Add(xform.Coordinates);
        }

        return _realmSpawns.Count > 0
            ? _random.Pick(_realmSpawns)
            : new EntityCoordinates(grid, new Vector2(0.5f, 0.5f));
    }

    private void ReturnFromRealm(Entity<StoriesHereticShadowRealmComponent> ent)
    {
        RemCompDeferred<StoriesHereticShadowRealmComponent>(ent);

        if (!_rule.TryGetRandomStationCoordinates(out var coords))
            coords = ent.Comp.ReturnCoordinates;

        if (!coords.IsValid(EntityManager))
            return;

        _transform.SetCoordinates(ent.Owner, coords);
        _transform.AttachToGridOrMap(ent.Owner);

        _popup.PopupEntity(Loc.GetString("stories-heretic-sacrifice-returned"), ent, ent, PopupType.Large);
    }

    private bool TryEnsureRealm([NotNullWhen(true)] out Entity<MapGridComponent>? grid)
    {
        if (_realmGrid is { } existing && !TerminatingOrDeleted(existing))
        {
            grid = existing;
            return true;
        }

        if (GetConfiguredRealmMap() is { } path
            && _mapLoader.TryLoadMap(path, out _, out var grids, new DeserializationOptions { InitializeMaps = true })
            && grids.Count > 0)
        {
            _realmGrid = grids.First();
            _realmFromFile = true;
        }
        else
        {
            var mapUid = _map.CreateMap(out var mapId);
            _atmosphere.SetMapAtmosphere(mapUid, false, CreateAir());

            var gravity = EnsureComp<GravityComponent>(mapUid);
            gravity.Enabled = true;
            gravity.Inherent = true;
            Dirty(mapUid, gravity);

            var light = EnsureComp<MapLightComponent>(mapUid);
            light.AmbientLightColor = RealmLight;
            Dirty(mapUid, light);

            _realmGrid = _map.CreateGridEntity(mapId);
            _realmFromFile = false;
        }

        grid = _realmGrid;
        return grid != null;
    }

    private ResPath? GetConfiguredRealmMap()
    {
        var query = EntityQueryEnumerator<StoriesHereticRuleComponent>();
        while (query.MoveNext(out var rule))
        {
            if (rule.ShadowRealmMap is { } path)
                return path;
        }

        return null;
    }

    private void BuildPlatform(Entity<MapGridComponent> grid, Vector2i center)
    {
        var tile = new Tile(_tileDefs[RealmTile].TileId);
        var tiles = new List<(Vector2i, Tile)>();
        for (var x = -PlatformRadius; x <= PlatformRadius; x++)
        {
            for (var y = -PlatformRadius; y <= PlatformRadius; y++)
            {
                tiles.Add((center + new Vector2i(x, y), tile));
            }
        }

        _map.SetTiles(grid, tiles);
    }

    private static GasMixture CreateAir()
    {
        var air = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
        air.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
        air.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);
        return air;
    }
}
