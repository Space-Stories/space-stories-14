using Content.Shared._Stories.Blackjack;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Stories.Blackjack.UI;

[UsedImplicitly]
public sealed partial class StoriesBlackjackLobbyBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;

    [ViewVariables]
    private StoriesBlackjackLobbyWindow? _window;

    private ResPath _tableSprite;
    private int _mine = -1;
    private bool _seated;
    private bool _ready;
    private bool _anyReady;
    private bool _lobby;
    private TimeSpan _phaseEnd;
    private int _timerSeconds = int.MinValue;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StoriesBlackjackLobbyWindow>();
        _window.OnSit += seat => SendMessage(new BlackjackSitMessage(seat));
        _window.OnReady += () => SendMessage(new BlackjackReadyMessage(!_ready));
        _window.OnLeave += () => SendMessage(new BlackjackLeaveMessage());
        _window.OnSpectate += () => SendMessage(new BlackjackSpectateMessage());
        _window.OnFrame += OnWindowFrame;
        ConnectCashier(_window.Cashier);
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table))
            return;

        var me = _player.LocalEntity is { } local ? EntMan.GetNetEntity(local) : NetEntity.Invalid;
        _window.SetTitle(Loc.GetString(table.Wager ? "stories-blackjack-window-title" : "stories-blackjack-window-title-prison"));

        if (_tableSprite != table.LobbySprite)
        {
            _tableSprite = table.LobbySprite;
            _window.SetTableSprite(_tableSprite);
        }

        var seats = table.Seats;
        var mine = BlackjackRules.FindSeat(seats, me);
        _mine = mine;

        _seated = mine >= 0;
        _ready = _seated && seats[mine].Ready;
        _lobby = table.Phase == BlackjackPhase.Lobby;
        _anyReady = false;
        var waiting = new List<string>();
        var occupied = 0;

        for (var i = 0; i < StoriesBlackjackLobbyWindow.SeatCount; i++)
        {
            if (i >= seats.Count)
            {
                _window.SetSeatHidden(i);
                continue;
            }

            var seat = seats[i];
            if (seat.Occupied)
            {
                occupied++;
                _anyReady |= seat.Ready;
                if (!seat.Ready)
                    waiting.Add(seat.Name);

                _window.SetSeatTaken(i, seat, i == mine, _lobby);
            }
            else
            {
                _window.SetSeatFree(i, !_seated);
            }
        }

        _phaseEnd = table.PhaseEnd;
        _window.SetStatus(StatusText(table.Phase, occupied, waiting));
        _timerSeconds = int.MinValue;
        UpdateTimer();
        UpdateSteps(table, mine);
        UpdateButtons(table, mine);
        UpdateRules(table);
        ReadHeld(table, out _cash, out _items);
        UpdateCashier(table, mine);
        UpdateWins(table);
    }

    private static string StatusText(BlackjackPhase phase, int occupied, List<string> waiting)
    {
        if (phase == BlackjackPhase.Idle || occupied == 0)
            return Loc.GetString("stories-blackjack-status-idle");

        if (phase == BlackjackPhase.Lobby && waiting.Count > 0)
            return Loc.GetString("stories-blackjack-status-waiting", ("names", string.Join(", ", waiting)));

        return Loc.GetString("stories-blackjack-status-playing");
    }

    private void OnWindowFrame()
    {
        UpdateTimer();
        UpdateHeld();
    }

    private void UpdateTimer()
    {
        if (_window == null)
            return;

        var show = _lobby && _anyReady;
        var seconds = show ? Math.Max(0, (int) Math.Ceiling((_phaseEnd - _timing.CurTime).TotalSeconds)) : -1;
        _window.SetTimerVisible(show);
        if (seconds == _timerSeconds)
            return;

        _timerSeconds = seconds;
        if (show)
            _window.SetTimerText(Loc.GetString("stories-blackjack-status-starting", ("seconds", seconds)));
    }

    private static bool HasChips(BlackjackTableComponent table, int mine)
    {
        return mine >= 0 && table.Seats[mine].Chips >= table.MinBet;
    }

    private void UpdateSteps(BlackjackTableComponent table, int mine)
    {
        if (_window == null)
            return;

        var show = table.Phase is BlackjackPhase.Idle or BlackjackPhase.Lobby;
        _window.SetSteps(show);
        if (!show)
            return;

        var readyIndex = table.Wager ? 2 : 1;
        var current = mine < 0 ? 0 : table.Wager && !HasChips(table, mine) ? 1 : _ready ? int.MaxValue : readyIndex;
        _window.SetStep(0, Loc.GetString("stories-blackjack-step-sit", ("n", 1)), StepState(0, current));
        if (table.Wager)
            _window.SetStep(1, Loc.GetString("stories-blackjack-step-buy", ("n", 2), ("min", table.MinBet)), StepState(1, current));

        _window.SetStep(readyIndex, Loc.GetString("stories-blackjack-step-ready", ("n", readyIndex + 1)), StepState(readyIndex, current));
        if (!table.Wager)
            _window.SetStep(2, null, StoriesBlackjackStepState.Pending);
    }

    private static StoriesBlackjackStepState StepState(int index, int current)
    {
        if (index < current)
            return StoriesBlackjackStepState.Done;

        return index == current ? StoriesBlackjackStepState.Current : StoriesBlackjackStepState.Pending;
    }

    private void UpdateButtons(BlackjackTableComponent table, int mine)
    {
        _window?.SetButtons(
            _seated && table.Phase == BlackjackPhase.Lobby,
            Loc.GetString(_ready ? "stories-blackjack-not-ready" : "stories-blackjack-ready"),
            !_ready && (!table.Wager || HasChips(table, mine)),
            !_ready,
            _seated,
            mine >= 0 && table.Seats[mine].Leaving,
            !_seated && table.Phase != BlackjackPhase.Idle && table.Phase != BlackjackPhase.Lobby);
    }

    private void UpdateRules(BlackjackTableComponent table)
    {
        var body = Loc.GetString(table.Wager ? "stories-blackjack-rules-body" : "stories-blackjack-rules-body-prison");
        var limits = table.MaxBet > 0
            ? Loc.GetString("stories-blackjack-rules-limits", ("min", table.MinBet), ("max", table.MaxBet))
            : Loc.GetString("stories-blackjack-rules-limits-min", ("min", table.MinBet));
        _window?.SetRules(body, table.Wager, table.Wager ? limits : string.Empty);
    }

    private void UpdateWins(BlackjackTableComponent table)
    {
        if (_window == null)
            return;

        if (table.Wager)
        {
            _window.SetWins(false, Array.Empty<KeyValuePair<string, int>>());
            return;
        }

        var rows = new List<KeyValuePair<string, int>>(table.Wins);
        rows.Sort((a, b) => b.Value.CompareTo(a.Value));
        _window.SetWins(true, rows);
    }
}
