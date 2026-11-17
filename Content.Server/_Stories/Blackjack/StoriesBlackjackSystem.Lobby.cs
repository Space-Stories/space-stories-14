using Content.Shared._Stories.Blackjack;

namespace Content.Server._Stories.Blackjack;

public sealed partial class StoriesBlackjackSystem
{
    private void OnSit(Entity<BlackjackTableComponent> ent, ref BlackjackSitMessage args)
    {
        var actor = args.Actor;
        if (HasComp<BlackjackPlayerComponent>(actor))
        {
            _popup.PopupEntity(Loc.GetString("stories-blackjack-popup-already-seated"), actor, actor);
            return;
        }

        if (args.Seat < 0 || args.Seat >= ent.Comp.Seats.Count || ent.Comp.Seats[args.Seat].Occupied)
        {
            _popup.PopupEntity(Loc.GetString("stories-blackjack-popup-seat-taken"), actor, actor);
            return;
        }

        ent.Comp.Seats[args.Seat] = new BlackjackSeat
        {
            Player = GetNetEntity(actor),
            Name = DisplayName(actor),
        };
        EnsureComp<BlackjackPlayerComponent>(actor).Table = ent;

        if (ent.Comp.Phase == BlackjackPhase.Idle)
        {
            ent.Comp.Phase = BlackjackPhase.Lobby;
            EnsureComp<ActiveBlackjackTableComponent>(ent);
        }
        else if (ent.Comp.Phase != BlackjackPhase.Lobby)
        {
            _ui.OpenUi(ent.Owner, BlackjackUiKey.Game, actor);
        }

        Refresh(ent);
    }

    private void OnReady(Entity<BlackjackTableComponent> ent, ref BlackjackReadyMessage args)
    {
        if (ent.Comp.Phase != BlackjackPhase.Lobby || !TryGetSeat(ent.Comp, args.Actor, out var index))
            return;

        var anyReady = AnyReady(ent.Comp);
        ent.Comp.Seats[index].Ready = args.Ready;
        if (args.Ready && !anyReady)
        {
            ent.Comp.PhaseEnd = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.ReadyTime);
            ent.Comp.PhaseDuration = ent.Comp.ReadyTime;
        }

        if (AllReady(ent.Comp))
            StartGame(ent);
        else
            Refresh(ent);
    }

    private void OnSpectate(Entity<BlackjackTableComponent> ent, ref BlackjackSpectateMessage args)
    {
        if (ent.Comp.Phase != BlackjackPhase.Idle && ent.Comp.Phase != BlackjackPhase.Lobby)
            _ui.OpenUi(ent.Owner, BlackjackUiKey.Game, args.Actor);
    }

    private void OnLeave(Entity<BlackjackTableComponent> ent, ref BlackjackLeaveMessage args)
    {
        if (TryGetSeat(ent.Comp, args.Actor, out var index))
            Leave(ent, index);
        else
            _ui.CloseUi(ent.Owner, BlackjackUiKey.Game, args.Actor);
    }

    private void Leave(Entity<BlackjackTableComponent> ent, int index)
    {
        var seat = ent.Comp.Seats[index];
        var inRound = ent.Comp.Phase is BlackjackPhase.Dealing or BlackjackPhase.Turns or BlackjackPhase.Dealer or BlackjackPhase.Payout;
        if (seat.Hands.Count > 0 && inRound)
        {
            seat.Leaving = true;
            Refresh(ent);
        }
        else
        {
            Unseat(ent, index);
        }
    }

    private bool AllReady(BlackjackTableComponent comp)
    {
        var any = false;
        foreach (var seat in comp.Seats)
        {
            if (!seat.Occupied)
                continue;

            if (!seat.Ready)
                return false;

            any = true;
        }

        return any;
    }

    private bool AnyReady(BlackjackTableComponent comp)
    {
        foreach (var seat in comp.Seats)
        {
            if (seat.Occupied && seat.Ready)
                return true;
        }

        return false;
    }

    private void UpdateLobby(Entity<BlackjackTableComponent> ent, TimeSpan now)
    {
        if (!AllReady(ent.Comp) && !(AnyReady(ent.Comp) && now >= ent.Comp.PhaseEnd))
            return;

        for (var i = 0; i < ent.Comp.Seats.Count; i++)
        {
            var seat = ent.Comp.Seats[i];
            if (seat.Occupied && !seat.Ready)
                Unseat(ent, i);
        }

        StartGame(ent);
    }

    private void StartGame(Entity<BlackjackTableComponent> ent)
    {
        ent.Comp.Phase = BlackjackPhase.Betting;
        Say(ent, "say-start");

        foreach (var seat in ent.Comp.Seats)
        {
            if (TryGetEntity(seat.Player, out var player))
                _ui.OpenUi(ent.Owner, BlackjackUiKey.Game, player.Value);

            seat.Ready = false;
        }

        _ui.CloseUi(ent.Owner, BlackjackUiKey.Lobby);
        BeginRound(ent);
    }

    private void Unseat(Entity<BlackjackTableComponent> ent, int index)
    {
        var seat = ent.Comp.Seats[index];
        if (seat.Occupied)
        {
            TryGetEntity(seat.Player, out var player);
            var amount = seat.Chips + seat.Bet;
            var spot = ent.Comp.SeatSpots[index];
            ent.Comp.Seats[index] = new BlackjackSeat();

            if (player != null)
                RemComp<BlackjackPlayerComponent>(player.Value);

            if (ent.Comp.Wager)
                GiveChips(ent, player, amount, spot);

            if (player != null)
                _ui.CloseUi(ent.Owner, BlackjackUiKey.Game, player.Value);
        }

        var anySeated = false;
        foreach (var other in ent.Comp.Seats)
        {
            anySeated |= other.Occupied;
        }

        if (!anySeated)
            ResetTable(ent);

        Refresh(ent);
    }

    private void ResetTable(Entity<BlackjackTableComponent> ent)
    {
        ent.Comp.Phase = BlackjackPhase.Idle;
        ent.Comp.ActiveSeat = -1;
        ent.Comp.DealerHand.Clear();
        ent.Comp.DealerHidden = false;
        ent.Comp.Pose = BlackjackPose.Idle;
        ent.Comp.Face = BlackjackFace.Neutral;
        ClearCards(ent);
        var shoe = Comp<BlackjackShoeComponent>(ent);
        shoe.Hole = null;
        shoe.Cards.Clear();
        RemCompDeferred<ActiveBlackjackTableComponent>(ent);
        _ui.CloseUi(ent.Owner, BlackjackUiKey.Game);
    }
}
