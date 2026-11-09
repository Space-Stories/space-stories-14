using Content.Shared._Stories.StationsControl;
using Robust.Client.UserInterface;

namespace Content.Client._Stories.StationsControl;

public sealed class StationsControlConsoleBoundUserInterface : BoundUserInterface
{
    private StationsControlConsoleWindow? _window;

    public StationsControlConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StationsControlConsoleWindow>();
        _window.SetAlertLevelRequested += (station, alertLevel) => SendMessage(new StationsControlConsoleSetAlertLevelMessage(station, alertLevel));
        _window.CallEvacRequested += time => SendMessage(new StationsControlConsoleCallEvacMessage(time));
        _window.RecallEvacRequested += () => SendMessage(new StationsControlConsoleRecallEvacMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is StationsControlConsoleState cast)
            _window?.UpdateState(cast);
    }
}