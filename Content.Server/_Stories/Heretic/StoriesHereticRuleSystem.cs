using System.Linq;
using Content.Server.Antag;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Atmos;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticRuleSystem : GameRuleSystem<StoriesHereticRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private StoriesHereticObjectiveSystem _objectives = default!;
    [Dependency] private StoriesHereticSacrificeSystem _sacrifice = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<StoriesHereticRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var query = EntityQueryEnumerator<StoriesHereticComponent>();
        while (query.MoveNext(out var other, out var otherComp))
        {
            if (other != args.EntityUid)
                _heretic.RemoveTarget((other, otherComp), args.EntityUid);
        }

        if (TryComp<StoriesHereticComponent>(args.EntityUid, out var heretic))
            _sacrifice.AssignTargets((args.EntityUid, heretic));

        var count = ent.Comp.InfluencesPerHeretic;
        if (!ent.Comp.InfluencesSpawned)
        {
            count += ent.Comp.BaseInfluences;
            ent.Comp.InfluencesSpawned = true;
        }

        for (var i = 0; i < count; i++)
        {
            if (TryGetRandomStationCoordinates(out var coords, ent.Comp))
                Spawn(ent.Comp.InfluencePrototype, coords);
        }
    }

    public bool TryGetRandomStationCoordinates(out EntityCoordinates coords, StoriesHereticRuleComponent? rule = null)
    {
        rule ??= EntityQuery<StoriesHereticRuleComponent>().FirstOrDefault() ?? new StoriesHereticRuleComponent();

        for (var i = 0; i < rule.TileAttempts; i++)
        {
            if (!TryFindRandomTile(out var index, out var station, out var grid, out coords))
                continue;

            if (!IsOnStationMap(station.Value, grid))
                continue;

            if (_turf.GetTileRef(coords) is not { } tile || _turf.IsSpace(tile))
                continue;

            if (_turf.IsTileBlocked(tile, CollisionGroup.Impassable))
                continue;

            if (!IsBreathable(_atmosphere.GetTileMixture(grid, null, index), rule.MinOxygenPressure))
                continue;

            return true;
        }

        coords = EntityCoordinates.Invalid;
        return false;
    }

    public bool IsOnStation(EntityUid uid)
    {
        return _station.GetOwningStation(uid) is { } station
               && Transform(uid).GridUid is { } grid
               && IsOnStationMap(station, grid);
    }

    private bool IsOnStationMap(EntityUid station, EntityUid grid)
    {
        if (HasComp<FTLComponent>(grid))
            return false;

        return _station.GetLargestGrid(station) is not { } main
               || Transform(grid).MapID == Transform(main).MapID;
    }

    private bool IsBreathable(GasMixture? air, float minOxygenPressure)
    {
        if (air == null || air.TotalMoles <= 0f)
            return false;

        var oxygen = air.Pressure * air.GetMoles(Gas.Oxygen) / air.TotalMoles;
        return air.Pressure is >= Atmospherics.WarningLowPressure and <= Atmospherics.WarningHighPressure
               && oxygen >= minOxygenPressure;
    }

    protected override void AppendRoundEndText(EntityUid uid,
        StoriesHereticRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var heretics = _antag.GetAntagIdentifiers(uid).ToList();
        args.AddLine(Loc.GetString("stories-heretic-round-end-count", ("count", heretics.Count)));

        foreach (var (mindId, data, name) in heretics)
        {
            if (!TryComp<MindComponent>(mindId, out var mind)
                || !_objectives.TryGetProgress(mindId, mind, out var heretic))
            {
                args.AddLine(Loc.GetString("stories-heretic-round-end-lost", ("name", name), ("user", data.UserName)));
                continue;
            }

            var path = heretic.Path is { } p
                ? Loc.GetString($"stories-heretic-path-{p.Id}")
                : Loc.GetString("stories-heretic-path-none");

            args.AddLine(Loc.GetString(heretic.Ascended ? "stories-heretic-round-end-ascended" : "stories-heretic-round-end-entry",
                ("name", name),
                ("user", data.UserName),
                ("path", path),
                ("knowledge", heretic.LearnedCount),
                ("sacrifices", heretic.SacrificeCount)));
        }

        args.AddLine("\n");
    }
}
