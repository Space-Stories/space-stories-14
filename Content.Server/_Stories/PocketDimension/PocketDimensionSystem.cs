using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Respawn;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared._Stories.PocketDimension;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Gravity;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Nuke;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Stories.PocketDimension;

public sealed partial class PocketDimensionSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SpecialRespawnSystem _respawn = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId DoorProto = "STPocketDimensionDoor";
    private static readonly EntProtoId RoomWallProto = "WallPlastitaniumIndestructible";
    private const string RoomFloor = "STFloorPocketDimension";
    private const string StickerContainer = "pocket_dimension_sticker";

    private const int RoomSize = 5;
    private static readonly Vector2i InnerDoorTile = new(2, 4);
    private static readonly Vector2i RoomCenterTile = new(2, 2);

    private static readonly TimeSpan StraySweepInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextStraySweep;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PocketDimensionStickerComponent, AfterInteractEvent>(OnStickerAfterInteract);
        SubscribeLocalEvent<PocketDimensionStickerComponent, PocketDimensionStickDoAfterEvent>(OnStickDoAfter);
        SubscribeLocalEvent<PocketDimensionStickerComponent, EntityTerminatingEvent>(OnStickerTerminating);

        SubscribeLocalEvent<PocketDimensionDoorComponent, ActivateInWorldEvent>(OnDoorActivate);
        SubscribeLocalEvent<PocketDimensionDoorComponent, PocketDimensionPassDoAfterEvent>(OnPassDoAfter);
        SubscribeLocalEvent<PocketDimensionDoorComponent, GetVerbsEvent<AlternativeVerb>>(OnDoorVerbs);
        SubscribeLocalEvent<PocketDimensionDoorComponent, PocketDimensionUnstickDoAfterEvent>(OnUnstickDoAfter);
        SubscribeLocalEvent<PocketDimensionDoorComponent, EntityTerminatingEvent>(OnDoorTerminating);

        SubscribeLocalEvent<PocketDimensionWallComponent, InteractHandEvent>(OnWallInteractHand);
        SubscribeLocalEvent<PocketDimensionWallComponent, InteractUsingEvent>(OnWallInteractUsing);
        SubscribeLocalEvent<PocketDimensionWallComponent, AttackedEvent>(OnWallAttacked);

        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);

        SubscribeLocalEvent<PortalComponent, MapInitEvent>(OnPortalMapInit);
    }

    #region Наклейка

    private void OnStickerAfterInteract(Entity<PocketDimensionStickerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } wall || !HasComp<WallComponent>(wall))
            return;

        args.Handled = true;

        if (!CanStickTo(wall))
        {
            _popup.PopupEntity(Loc.GetString("st-pocket-dimension-bad-wall"), args.User, args.User);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.StickDelay, new PocketDimensionStickDoAfterEvent(), ent, target: wall, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    private bool CanStickTo(EntityUid wall)
    {
        return Transform(wall).GridUid is { } grid
               && _station.GetOwningStation(grid) != null
               && !HasComp<ArrivalsShuttleComponent>(grid)
               && !HasComp<EmergencyShuttleComponent>(grid);
    }

    private void OnStickDoAfter(Entity<PocketDimensionStickerComponent> ent, ref PocketDimensionStickDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } wall)
            return;

        args.Handled = true;
        Stick(ent, wall, args.User);
    }

    public void Stick(Entity<PocketDimensionStickerComponent> ent, EntityUid wall, EntityUid user)
    {
        EnsureRoom(ent);

        var door = Spawn(DoorProto, Transform(wall).Coordinates);
        Comp<PocketDimensionDoorComponent>(door).Sticker = ent;
        _container.Insert(ent.Owner, _container.EnsureContainer<ContainerSlot>(door, StickerContainer));

        ent.Comp.OuterDoor = door;
        ent.Comp.Exit = Transform(user).Coordinates;
        _audio.PlayPvs(ent.Comp.StickSound, door);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} stuck {ToPrettyString(ent):entity} on {ToPrettyString(wall):entity}");
    }

    private void EnsureRoom(Entity<PocketDimensionStickerComponent> ent)
    {
        if (Exists(ent.Comp.Map))
            return;

        var mapUid = _map.CreateMap(out var mapId);
        AddComp(mapUid, new PocketDimensionMapComponent { Sticker = ent });

        var grid = EnsureComp<MapGridComponent>(mapUid);
        var gravity = EnsureComp<GravityComponent>(mapUid);
        gravity.Enabled = true;
        gravity.Inherent = true;
        Dirty(mapUid, gravity);
        _map.SetAmbientLight(mapId, Color.FromHex("#4a4060"));

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int) Gas.Oxygen] = Atmospherics.OxygenMolesStandard;
        moles[(int) Gas.Nitrogen] = Atmospherics.NitrogenMolesStandard;
        _atmos.SetMapAtmosphere(mapUid, false, new GasMixture(moles, Atmospherics.T20C));

        var floor = new Tile(_tileDef[RoomFloor].TileId);
        var tiles = new List<(Vector2i, Tile)>();
        for (var x = 0; x < RoomSize; x++)
        {
            for (var y = 0; y < RoomSize; y++)
            {
                tiles.Add((new Vector2i(x, y), floor));
            }
        }
        _map.SetTiles((mapUid, grid), tiles);

        foreach (var (pos, _) in tiles)
        {
            if (pos.X is > 0 and < RoomSize - 1 && pos.Y is > 0 and < RoomSize - 1)
                continue;

            var wall = Spawn(RoomWallProto, _map.GridTileToLocal(mapUid, grid, pos));
            EnsureComp<PocketDimensionWallComponent>(wall);
            EnsureComp<DamageableComponent>(wall);
        }

        ent.Comp.Map = mapUid;
        EnsureInnerDoor(ent);
    }

    private EntityUid? EnsureInnerDoor(Entity<PocketDimensionStickerComponent> ent)
    {
        if (Exists(ent.Comp.InnerDoor))
            return ent.Comp.InnerDoor;

        if (!TryComp<MapGridComponent>(ent.Comp.Map, out var grid))
            return null;

        var door = Spawn(DoorProto, _map.GridTileToLocal(ent.Comp.Map.Value, grid, InnerDoorTile));
        Comp<PocketDimensionDoorComponent>(door).Sticker = ent;
        ent.Comp.InnerDoor = door;
        return door;
    }

    private void OnStickerTerminating(Entity<PocketDimensionStickerComponent> ent, ref EntityTerminatingEvent args)
    {
        QueueDel(ent.Comp.OuterDoor);

        if (!Exists(ent.Comp.Map))
            return;

        var dest = GetEjectCoordinates(ent);

        if (dest != null && (TerminatingOrDeleted(_map.GetMapOrInvalid(dest.Value.MapId))
                             || _map.TryFindGridAt(dest.Value, out var destGrid, out _) && TerminatingOrDeleted(destGrid)))
            dest = null;

        var toEject = new List<EntityUid>();
        var children = Transform(ent.Comp.Map.Value).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!Transform(child).Anchored)
                toEject.Add(child);
        }

        foreach (var uid in toEject)
        {
            if (dest != null)
                _transform.SetMapCoordinates(uid, dest.Value);
        }

        QueueDel(ent.Comp.Map);
    }

    private MapCoordinates? GetEjectCoordinates(Entity<PocketDimensionStickerComponent> ent)
    {
        if (ent.Comp.OuterDoor != null && ent.Comp.Exit is { } exit)
            return _transform.ToMapCoordinates(exit);

        var own = _transform.GetMapCoordinates(ent);
        if (own.MapId != MapId.Nullspace && own.MapId != Transform(ent.Comp.Map!.Value).MapID)
            return own;

        return TryGetRandomStationCoordinates(out var coords) ? _transform.ToMapCoordinates(coords) : null;
    }

    #endregion

    #region Двери

    private void OnDoorActivate(Entity<PocketDimensionDoorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        var started = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.PassDelay, new PocketDimensionPassDoAfterEvent(), ent, target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });

        if (started)
            _appearance.SetData(ent, PocketDimensionDoorVisuals.Open, true);
    }

    private void OnPassDoAfter(Entity<PocketDimensionDoorComponent> ent, ref PocketDimensionPassDoAfterEvent args)
    {
        _appearance.SetData(ent, PocketDimensionDoorVisuals.Open, false);

        if (args.Handled || args.Cancelled || !TryComp<PocketDimensionStickerComponent>(ent.Comp.Sticker, out var sticker))
            return;

        args.Handled = true;

        if (ent.Owner == sticker.InnerDoor)
        {
            if (sticker.OuterDoor == null || sticker.Exit is not { } exit)
            {
                _popup.PopupEntity(Loc.GetString("st-pocket-dimension-nothing-outside"), args.User, args.User);
                return;
            }

            _audio.PlayPvs(ent.Comp.PassSound, ent);
            Teleport(args.User, exit);
            _audio.PlayPvs(ent.Comp.PassSound, exit);
            return;
        }

        if (EnsureInnerDoor((ent.Comp.Sticker!.Value, sticker)) is not { } inner)
            return;

        _audio.PlayPvs(ent.Comp.PassSound, ent);
        Teleport(args.User, Transform(inner).Coordinates.Offset(new Vector2(0, -1)));
        _audio.PlayPvs(ent.Comp.PassSound, inner);
        EjectNukeDisks(ent.Comp.Sticker!.Value, sticker);

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):player} entered the pocket dimension of {ToPrettyString(ent.Comp.Sticker):entity}");
    }

    private void Teleport(EntityUid user, EntityCoordinates coords)
    {
        var pulled = CompOrNull<PullerComponent>(user)?.Pulling;
        _transform.SetCoordinates(user, coords);
        if (pulled != null)
            _transform.SetCoordinates(pulled.Value, coords);
    }

    private void EjectNukeDisks(EntityUid sticker, PocketDimensionStickerComponent comp)
    {
        if (comp.Exit is not { } exit)
            return;

        var query = EntityQueryEnumerator<NukeDiskComponent, TransformComponent>();
        while (query.MoveNext(out var disk, out _, out var xform))
        {
            if (xform.MapUid != comp.Map)
                continue;

            _container.TryRemoveFromContainer(disk, true);
            _transform.SetCoordinates(disk, exit);
            _popup.PopupCoordinates(Loc.GetString("st-pocket-dimension-disk-ejected"), exit);
        }
    }

    private void OnDoorVerbs(Entity<PocketDimensionDoorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !IsOuterDoor(ent, out var sticker))
            return;

        var user = args.User;
        var delay = sticker.UnstickDelay;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("st-pocket-dimension-unstick-verb"),
            Act = () => _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new PocketDimensionUnstickDoAfterEvent(), ent, target: ent)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            }),
        });
    }

    private void OnUnstickDoAfter(Entity<PocketDimensionDoorComponent> ent, ref PocketDimensionUnstickDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !IsOuterDoor(ent, out var sticker))
            return;

        args.Handled = true;
        _audio.PlayPvs(sticker.StickSound, ent);
        sticker.OuterDoor = null;
        sticker.Exit = null;
        _hands.PickupOrDrop(args.User, ent.Comp.Sticker!.Value);
        QueueDel(ent);
    }

    private void OnDoorTerminating(Entity<PocketDimensionDoorComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!IsOuterDoor(ent, out var sticker))
            return;

        sticker.OuterDoor = null;
        sticker.Exit = null;

        if (TerminatingOrDeleted(Transform(ent).ParentUid))
            return;

        _container.TryRemoveFromContainer(ent.Comp.Sticker!.Value, true);
    }

    private bool IsOuterDoor(Entity<PocketDimensionDoorComponent> ent, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PocketDimensionStickerComponent? sticker)
    {
        return TryComp(ent.Comp.Sticker, out sticker) && sticker.OuterDoor == ent.Owner;
    }

    #endregion

    #region Побег через стену

    private void OnWallInteractHand(Entity<PocketDimensionWallComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        HitWall(ent, args.User);
    }

    private void OnWallInteractUsing(Entity<PocketDimensionWallComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        HitWall(ent, args.User);
    }

    private void OnWallAttacked(Entity<PocketDimensionWallComponent> ent, ref AttackedEvent args)
    {
        HitWall(ent, args.User);
    }

    private void HitWall(Entity<PocketDimensionWallComponent> ent, EntityUid user)
    {
        if (!TryComp<PocketDimensionMapComponent>(Transform(ent).MapUid, out var room) || _timing.CurTime < room.NextHit)
            return;

        room.NextHit = _timing.CurTime + ent.Comp.HitCooldown;
        room.Hits++;
        _audio.PlayPvs(ent.Comp.HitSound, ent);

        if (room.Hits < ent.Comp.HitsRequired)
        {
            _popup.PopupEntity(Loc.GetString("st-pocket-dimension-hit", ("hits", room.Hits), ("required", ent.Comp.HitsRequired)), user, user);
            return;
        }

        if (!TryGetRandomStationCoordinates(out var coords))
        {
            _popup.PopupEntity(Loc.GetString("st-pocket-dimension-break-fail"), user, user);
            return;
        }

        room.Hits = 0;
        Teleport(user, coords);
        _popup.PopupEntity(Loc.GetString("st-pocket-dimension-break-done"), user, user);
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):player} broke out of a pocket dimension");
    }

    private bool TryGetRandomStationCoordinates(out EntityCoordinates coords)
    {
        coords = EntityCoordinates.Invalid;
        foreach (var station in _station.GetStations())
        {
            if (_station.GetLargestGrid(station) is { } grid
                && Transform(grid).MapUid is { } map
                && _respawn.TryFindRandomTile(grid, map, 20, out coords))
                return true;
        }

        return false;
    }

    #endregion

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (InPocketDimension(args.RadioSource))
            args.Cancelled = true;
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (InPocketDimension(args.RadioReceiver))
            args.Cancelled = true;
    }

    private void OnPortalMapInit(Entity<PortalComponent> ent, ref MapInitEvent args)
    {
        if (!InPocketDimension(ent))
            return;

        _physics.SetCanCollide(ent, false);
        _popup.PopupCoordinates(Loc.GetString("portal-component-invalid-configuration-fizzle"), Transform(ent).Coordinates, PopupType.Medium);
        QueueDel(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextStraySweep)
            return;

        _nextStraySweep = _timing.CurTime + StraySweepInterval;

        var stray = new List<EntityUid>();
        var query = EntityQueryEnumerator<PocketDimensionMapComponent, MapGridComponent>();
        while (query.MoveNext(out var map, out _, out var grid))
        {
            stray.Clear();
            var children = Transform(map).ChildEnumerator;
            while (children.MoveNext(out var child))
            {
                var xform = Transform(child);
                if (xform.Anchored)
                    continue;

                var tile = _map.LocalToTile(map, grid, xform.Coordinates);
                if (tile.X is < 1 or > RoomSize - 2 || tile.Y is < 1 or > RoomSize - 2)
                    stray.Add(child);
            }

            var center = _map.GridTileToLocal(map, grid, RoomCenterTile);
            foreach (var uid in stray)
            {
                _transform.SetCoordinates(uid, center);
            }
        }
    }

    private bool InPocketDimension(EntityUid uid)
    {
        return HasComp<PocketDimensionMapComponent>(Transform(uid).MapUid);
    }
}
