using Robust.Shared.Serialization;

namespace Content.Shared._Stories.StationsControl;

[Serializable, NetSerializable] 
public enum StationsControlConsoleUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class StationsControlConsoleState : BoundUserInterfaceState
{
    public List<string> StationsList { get; }
    public List<NetEntity> StationsNetIds { get; }

    public StationsControlConsoleState(List<string> stationsList, List<NetEntity> stationsNetIds)
    {
        StationsList = stationsList;
        StationsNetIds = stationsNetIds;
    }
}

[Serializable, NetSerializable]
public sealed class StationsControlConsoleSetAlertLevelMessage : BoundUserInterfaceMessage
{
    public NetEntity Station { get; }
    public string AlertLevel { get; }

    public StationsControlConsoleSetAlertLevelMessage(NetEntity station, string alertLevel)
    {
        Station = station;
        AlertLevel = alertLevel;
    }
}

[Serializable, NetSerializable]
public sealed class StationsControlConsoleCallEvacMessage : BoundUserInterfaceMessage
{
    public TimeSpan CountdownTime { get; }
    public StationsControlConsoleCallEvacMessage(TimeSpan countdownTime) { CountdownTime = countdownTime; }
}

[Serializable, NetSerializable]
public sealed class StationsControlConsoleRecallEvacMessage : BoundUserInterfaceMessage
{
    public StationsControlConsoleRecallEvacMessage() { }
}