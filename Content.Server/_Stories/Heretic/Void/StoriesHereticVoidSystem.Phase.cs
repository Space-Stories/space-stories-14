using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    private readonly HashSet<Entity<MobStateComponent>> _phaseMobs = new();

    private void InitializePhase()
    {
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticVoidPhaseEvent>(OnVoidPhase);
    }

    private void OnVoidPhase(Entity<StoriesHereticComponent> ent, ref StoriesHereticVoidPhaseEvent args)
    {
        if (args.Handled)
            return;

        var origin = Transform(ent).Coordinates;
        var target = args.Target;
        var distance = (_transform.ToMapCoordinates(target).Position - _transform.ToMapCoordinates(origin).Position).Length();
        if (distance < args.MinRange
            || distance > args.MaxRange
            || !IsTileFree(target)
            || !_examine.InRangeUnOccluded(ent, target, args.MaxRange + 1f))
            return;

        args.Handled = true;

        _chat.TrySendInGameICMessage(ent, Loc.GetString(args.Speech), InGameICChatType.Whisper, false);

        PhaseBurst(ent, origin, args);
        _transform.SetCoordinates(ent, target);
        _transform.AttachToGridOrMap(ent);
        PhaseBurst(ent, target, args);

        Spawn(args.EffectOut, origin);
        Spawn(args.EffectIn, target);
        _audio.PlayPvs(args.Sound, origin);
        _audio.PlayPvs(args.Sound, target);
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

    private void PhaseBurst(EntityUid caster, EntityCoordinates coords, StoriesHereticVoidPhaseEvent args)
    {
        _phaseMobs.Clear();
        _lookup.GetEntitiesInRange(coords, args.Radius, _phaseMobs);
        foreach (var mob in _phaseMobs)
        {
            if (mob.Owner == caster
                || HasComp<StoriesHereticComponent>(mob)
                || _heretic.IsServant(mob))
                continue;

            _damageable.TryChangeDamage(mob.Owner, args.Damage, origin: caster);
            _chill.ApplyChill(mob, args.ChillStacks);
        }
    }
}
