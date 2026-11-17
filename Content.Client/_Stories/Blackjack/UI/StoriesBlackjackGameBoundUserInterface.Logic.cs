using Content.Shared._Stories.Blackjack;

namespace Content.Client._Stories.Blackjack.UI;

public sealed partial class StoriesBlackjackGameBoundUserInterface
{
    private BlackjackTableComponent? Table()
    {
        return EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table) ? table : null;
    }

    private BlackjackSeat? MySeat()
    {
        if (Table() is not { } table || _mySeat < 0 || _mySeat >= table.Seats.Count)
            return null;

        return table.Seats[_mySeat];
    }

    private int BetCap()
    {
        if (Table() is not { } table || MySeat() is not { } seat)
            return 0;

        return table.MaxBet > 0 ? Math.Min(seat.Chips, table.MaxBet) : seat.Chips;
    }

    private BlackjackHand? ActiveHand()
    {
        if (Table() is not { } table || MySeat() is not { } seat)
            return null;

        if (table.Phase != BlackjackPhase.Turns || table.ActiveSeat != _mySeat)
            return null;

        if (table.ActiveHand < 0 || table.ActiveHand >= seat.Hands.Count)
            return null;

        var hand = seat.Hands[table.ActiveHand];
        return hand.State == BlackjackHandState.Playing ? hand : null;
    }

    private Snapshot Take()
    {
        if (Table() is not { } table)
            return default;

        BlackjackHand? hand = null;
        if (table.ActiveSeat >= 0 && table.ActiveSeat < table.Seats.Count)
        {
            var hands = table.Seats[table.ActiveSeat].Hands;
            if (table.ActiveHand >= 0 && table.ActiveHand < hands.Count)
                hand = hands[table.ActiveHand];
        }

        var seat = MySeat();
        return new Snapshot(
            table.Phase,
            table.ActiveSeat,
            table.ActiveHand,
            hand?.Cards.Count ?? -1,
            hand?.State ?? default,
            seat?.Bet ?? 0,
            seat?.Chips ?? 0,
            seat?.Hands.Count ?? 0);
    }

    private void Lock()
    {
        _locked = true;
        _lockSnapshot = Take();
        _lockUntil = _timing.RealTime + TimeSpan.FromSeconds(LockTime);
    }

    private void SetSum(int value)
    {
        _betSum = Math.Clamp(value, 0, BetCap());
        Refresh();
    }

    private void PlaceBet()
    {
        if (_locked || Table() is not { } table || MySeat() is not { } seat)
            return;

        if (!table.Wager || table.Phase != BlackjackPhase.Betting || seat.Bet != 0)
            return;

        if (_betSum < table.MinBet || _betSum > BetCap())
            return;

        var amount = _betSum;
        Lock();
        SendMessage(new BlackjackBetMessage(amount));
        Refresh();
    }

    private void SendAction(BlackjackAction action)
    {
        if (_locked || Table() is not { } table || ActiveHand() == null)
            return;

        Lock();
        SendMessage(new BlackjackActionMessage(action, table.ActiveHand));
        Refresh();
    }

    private void OnCollapsePressed()
    {
        _collapsed = !_collapsed;
        Refresh();
    }

    private void OnCashierPressed()
    {
        SendMessage(new BlackjackOpenCashierMessage());
    }

    private void OnLeavePressed()
    {
        if (MySeat() is not { } seat)
        {
            Close();
            return;
        }

        if (!seat.Leaving)
            SendMessage(new BlackjackLeaveMessage());
    }

    private void OnPanelFrame()
    {
        if (_panel == null || Table() == null)
            return;

        if (_locked && _timing.RealTime >= _lockUntil)
        {
            _locked = false;
            Refresh();
            return;
        }

        UpdateTimer();
        if (_countdown == int.MinValue)
            return;

        var seconds = CountdownSeconds();
        if (seconds == _countdown)
            return;

        _countdown = seconds;
        _panel.SetStatusText(Loc.GetString("stories-blackjack-panel-next", ("seconds", Math.Max(seconds, 0))));
    }

    private int CountdownSeconds()
    {
        if (Table() is not { } table)
            return 0;

        return (int) Math.Ceiling((table.PhaseEnd - _timing.CurTime).TotalSeconds);
    }

    private void UpdateTimer()
    {
        if (_panel == null || Table() is not { } table)
            return;

        var show = !_collapsed
                   && (table.Phase == BlackjackPhase.Betting || table.Phase == BlackjackPhase.Turns)
                   && table.PhaseDuration > 0f;
        if (!show)
        {
            _panel.SetTimer(false, 0f, false);
            return;
        }

        var left = (float) (table.PhaseEnd - _timing.CurTime).TotalSeconds;
        var fraction = Math.Clamp(left / table.PhaseDuration, 0f, 1f);
        _panel.SetTimer(true, fraction, fraction < LowTimer);
    }

    private void Refresh()
    {
        if (_panel == null || Table() is not { } table)
            return;

        var seat = MySeat();
        var hand = ActiveHand();
        var wager = table.Wager;

        _panel.SetHeader(
            Loc.GetString(wager ? "stories-blackjack-window-title" : "stories-blackjack-window-title-prison"),
            wager && seat != null ? Loc.GetString("stories-blackjack-panel-stack", ("amount", seat.Chips)) : null,
            wager && seat != null,
            Loc.GetString(_collapsed ? "stories-blackjack-panel-expand" : "stories-blackjack-panel-collapse"),
            Loc.GetString(seat != null ? "stories-blackjack-panel-leave" : "stories-blackjack-panel-close"),
            seat == null || !seat.Leaving);
        _panel.SetCollapsed(_collapsed, hand != null);

        _countdown = int.MinValue;
        if (seat is { Leaving: true })
        {
            _panel.ShowStatus(Loc.GetString("stories-blackjack-panel-leaving"));
        }
        else if (table.Phase == BlackjackPhase.Betting && wager && seat != null && seat.Bet == 0)
        {
            if (seat.Chips < table.MinBet)
                _panel.ShowStatus(Loc.GetString("stories-blackjack-panel-no-chips", ("amount", table.MinBet)));
            else
                RefreshBet(table, seat);
        }
        else if (hand != null && seat != null)
        {
            var funds = !wager || seat.Chips >= hand.Bet;
            _panel.ShowActions(
                !_locked,
                !_locked,
                wager,
                !_locked && BlackjackRules.CanDouble(hand) && funds,
                !_locked && BlackjackRules.CanSplit(seat, hand) && funds);
        }
        else
        {
            _panel.ShowStatus(StatusText(table, seat));
        }

        UpdateTimer();
    }

    private void RefreshBet(BlackjackTableComponent table, BlackjackSeat seat)
    {
        if (_panel == null)
            return;

        var cap = BetCap();
        _betSum = Math.Min(_betSum, cap);
        _panel.ShowBet(
            Loc.GetString("stories-blackjack-panel-bet", ("amount", _betSum)),
            !_locked && _betSum < cap,
            !_locked && _betSum >= table.MinBet && _betSum <= cap,
            !_locked && seat.LastBet >= table.MinBet && seat.LastBet <= cap,
            !_locked && cap >= table.MinBet && _betSum != cap,
            !_locked && _betSum > 0);
    }

    private string StatusText(BlackjackTableComponent table, BlackjackSeat? seat)
    {
        switch (table.Phase)
        {
            case BlackjackPhase.Betting:
                if (!table.Wager)
                {
                    _countdown = CountdownSeconds();
                    return Loc.GetString("stories-blackjack-panel-next", ("seconds", Math.Max(_countdown, 0)));
                }

                if (seat != null && seat.Bet > 0)
                    return Loc.GetString("stories-blackjack-panel-bet-placed", ("amount", seat.Bet));

                return Loc.GetString("stories-blackjack-panel-betting");
            case BlackjackPhase.Dealing:
                return Loc.GetString("stories-blackjack-status-playing");
            case BlackjackPhase.Turns:
                if (table.ActiveSeat >= 0 && table.ActiveSeat < table.Seats.Count)
                    return Loc.GetString("stories-blackjack-panel-turn-of", ("name", table.Seats[table.ActiveSeat].Name));

                return Loc.GetString("stories-blackjack-status-playing");
            case BlackjackPhase.Dealer:
                return Loc.GetString("stories-blackjack-panel-dealer-turn");
            case BlackjackPhase.Payout:
                return Loc.GetString("stories-blackjack-panel-payout");
            default:
                return Loc.GetString("stories-blackjack-status-idle");
        }
    }
}
