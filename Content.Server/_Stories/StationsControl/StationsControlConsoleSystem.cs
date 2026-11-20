using Content.Server.RoundEnd;
using Content.Shared._Stories.StationsControl;
using Content.Shared.AlertLevel;
using Content.Shared.Station;
using Robust.Server.GameObjects;

namespace Content.Server._Stories.StationsControl;

public sealed partial class StationsControlConsoleSystem : EntitySystem
{
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndSystemChangedEvent>(_ => OnRoundEndChanged());
        SubscribeLocalEvent<StationsControlConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);

        Subs.BuiEvents<StationsControlConsoleComponent>(StationsControlConsoleUiKey.Key, subs =>
        {
            subs.Event<StationsControlConsoleSetAlertLevelMessage>(OnSetCode);
            subs.Event<StationsControlConsoleCallEvacMessage>(OnCallEvac);
            subs.Event<StationsControlConsoleRecallEvacMessage>(OnRecallEvac);
        });
    }

    private void OnUiOpened(EntityUid uid, StationsControlConsoleComponent comp, BoundUIOpenedEvent args)
    {
        var stationsList = new List<string>();
        var stationsNetIds = new List<NetEntity>();

        foreach (var station in _station.GetStations())
        {
            var meta = MetaData(station);
            stationsList.Add(meta.EntityName);
            stationsNetIds.Add(GetNetEntity(station));
        }

        var shuttleTimeLeft = _roundEnd.ShuttleTimeLeft;

        var state = new StationsControlConsoleState(
            stationsList,
            stationsNetIds,
            comp.AllowedAlertLevels,
            comp.AllowAnyAlertLevel,
            shuttleTimeLeft);
        _uiSystem.SetUiState(uid, StationsControlConsoleUiKey.Key, state);
    }

    private void OnSetCode(EntityUid uid, StationsControlConsoleComponent comp, StationsControlConsoleSetAlertLevelMessage args)
    {
        var stationUid = GetEntity(args.Station);
        _alertLevel.SetLevel(stationUid, args.AlertLevel, true, true, true);
    }

    private void OnCallEvac(EntityUid uid, StationsControlConsoleComponent comp, StationsControlConsoleCallEvacMessage args)
    {
        _roundEnd.RequestRoundEnd(
            args.CountdownTime,
            requester: null,
            machine: uid,
            checkCooldown: false
        );
    }

    private void OnRecallEvac(EntityUid uid, StationsControlConsoleComponent comp, StationsControlConsoleRecallEvacMessage args)
    {
        _roundEnd.CancelRoundEndCountdown(
            requester: args.Actor,
            machine: uid,
            forceRecall: true
        );
    }

    private void OnRoundEndChanged()
    {
        var query = AllEntityQuery<StationsControlConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var shuttleTimeLeft = _roundEnd.ShuttleTimeLeft;
            var stationsList = new List<string>();
            var stationsNetIds = new List<NetEntity>();
            foreach (var station in _station.GetStations())
            {
                var meta = MetaData(station);
                stationsList.Add(meta.EntityName);
                stationsNetIds.Add(GetNetEntity(station));
            }

            var state = new StationsControlConsoleState(
                stationsList,
                stationsNetIds,
                comp.AllowedAlertLevels,
                comp.AllowAnyAlertLevel,
                shuttleTimeLeft
            );

            _uiSystem.SetUiState(uid, StationsControlConsoleUiKey.Key, state);
        }
    }
}
