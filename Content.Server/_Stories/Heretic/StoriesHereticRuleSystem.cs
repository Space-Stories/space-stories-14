using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Robust.Shared.Map;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticRuleSystem : GameRuleSystem<StoriesHereticRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private StoriesHereticSacrificeSystem _sacrifice = default!;
    [Dependency] private TurfSystem _turf = default!;

    private const int TileAttempts = 10;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<StoriesHereticRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
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
            if (TryGetRandomStationCoordinates(out var coords))
                Spawn(ent.Comp.InfluencePrototype, coords);
        }
    }

    /// <summary>
    /// Случайная проходимая плитка станции.
    /// </summary>
    public bool TryGetRandomStationCoordinates(out EntityCoordinates coords)
    {
        for (var i = 0; i < TileAttempts; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out coords))
                continue;

            if (_turf.GetTileRef(coords) is not { } tile || _turf.IsSpace(tile))
                continue;

            if (_turf.IsTileBlocked(tile, CollisionGroup.Impassable))
                continue;

            return true;
        }

        coords = EntityCoordinates.Invalid;
        return false;
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
                || mind.OwnedEntity is not { } body
                || !TryComp<StoriesHereticComponent>(body, out var heretic))
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
