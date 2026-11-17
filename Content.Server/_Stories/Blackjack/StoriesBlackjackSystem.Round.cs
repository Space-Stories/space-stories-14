using System.Text;
using Content.Shared._Stories.Blackjack;
using Content.Shared.Chat;
using Content.Shared.Database;

namespace Content.Server._Stories.Blackjack;

public sealed partial class StoriesBlackjackSystem
{
    private void OnBet(Entity<BlackjackTableComponent> ent, ref BlackjackBetMessage args)
    {
        var comp = ent.Comp;
        if (!comp.Wager || comp.Phase != BlackjackPhase.Betting || !TryGetSeat(comp, args.Actor, out var index))
            return;

        var seat = comp.Seats[index];
        var amount = args.Amount;
        if (seat.Bet != 0 || amount < comp.MinBet || amount > seat.Chips || (comp.MaxBet > 0 && amount > comp.MaxBet))
            return;

        seat.Chips -= amount;
        seat.Bet = amount;
        seat.LastBet = amount;
        seat.Missed = 0;
        _audio.PlayPvs(comp.ChipSound, ent);
        Refresh(ent);

        if (AllBet(comp))
            EndBetting(ent);
    }

    private bool AllBet(BlackjackTableComponent comp)
    {
        var any = false;
        foreach (var seat in comp.Seats)
        {
            if (!seat.Occupied)
                continue;

            if (seat.Bet == 0)
                return false;

            any = true;
        }

        return any;
    }

    private int AtStake(BlackjackSeat seat)
    {
        var amount = seat.Bet;
        foreach (var hand in seat.Hands)
        {
            if (hand.Result == BlackjackResult.None && hand.State != BlackjackHandState.Bust)
                amount += hand.Bet;
        }

        return amount;
    }

    private void OnAction(Entity<BlackjackTableComponent> ent, ref BlackjackActionMessage args)
    {
        if (args.Hand == ent.Comp.ActiveHand && TryGetSeat(ent.Comp, args.Actor, out var index))
            TryAction(ent, index, args.Action);
    }

    private void OnPlayerSpoke(Entity<BlackjackPlayerComponent> ent, ref EntitySpokeEvent args)
    {
        var comp = ent.Comp;
        if (!TryComp(comp.Table, out BlackjackTableComponent? table) || table.Phase != BlackjackPhase.Turns)
            return;

        if (!TryGetSeat(table, ent, out var index) || index != table.ActiveSeat)
            return;

        if (args.Message.Contains('?'))
            return;

        var text = NormalizeVoice(args.Message);
        var tableEnt = new Entity<BlackjackTableComponent>(comp.Table, table);
        if (HasVoicePhrase(text, "stories-blackjack-voice-stand"))
            TryAction(tableEnt, index, BlackjackAction.Stand);
        else if (HasVoicePhrase(text, "stories-blackjack-voice-split"))
            TryAction(tableEnt, index, BlackjackAction.Split);
        else if (HasVoicePhrase(text, "stories-blackjack-voice-double"))
            TryAction(tableEnt, index, BlackjackAction.Double);
        else if (HasVoicePhrase(text, "stories-blackjack-voice-hit"))
            TryAction(tableEnt, index, BlackjackAction.Hit);
    }

    private static string NormalizeVoice(string message)
    {
        var builder = new StringBuilder(message.Length + 2);
        builder.Append(' ');
        var last = ' ';
        foreach (var raw in message.ToLowerInvariant())
        {
            var symbol = raw == 'ё' ? 'е' : raw;
            if (!char.IsLetter(symbol))
                symbol = ' ';

            if (symbol == last)
                continue;

            builder.Append(symbol);
            last = symbol;
        }

        if (last != ' ')
            builder.Append(' ');

        return builder.ToString();
    }

    private bool HasVoicePhrase(string text, string key)
    {
        foreach (var variant in Loc.GetString(key).Split(','))
        {
            var phrase = NormalizeVoice(variant);
            if (phrase.Length > 1 && text.Contains(phrase))
                return true;
        }

        return false;
    }

    private BlackjackCard Draw(Entity<BlackjackTableComponent> ent)
    {
        var shoe = Comp<BlackjackShoeComponent>(ent);
        if (shoe.Cards.Count == 0)
            Reshuffle(ent);

        var last = shoe.Cards.Count - 1;
        var card = shoe.Cards[last];
        shoe.Cards.RemoveAt(last);
        return card;
    }

    private void Reshuffle(Entity<BlackjackTableComponent> ent)
    {
        var shoe = Comp<BlackjackShoeComponent>(ent);
        shoe.Cards.Clear();
        for (var deck = 0; deck < ent.Comp.Decks; deck++)
        {
            for (var suit = BlackjackSuit.Clubs; suit <= BlackjackSuit.Spades; suit++)
            {
                for (byte rank = 1; rank <= 13; rank++)
                {
                    shoe.Cards.Add(new BlackjackCard(suit, rank));
                }
            }
        }

        _random.Shuffle(shoe.Cards);
        shoe.Size = shoe.Cards.Count;
        Say(ent, "say-shuffle");
        _audio.PlayPvs(ent.Comp.ShuffleSound, ent);
        SetPose(ent, BlackjackPose.Shuffle, ent.Comp.ShufflePoseTime);
    }

    private bool NeedsReshuffle(Entity<BlackjackTableComponent> ent)
    {
        var shoe = Comp<BlackjackShoeComponent>(ent);
        return shoe.Cards.Count == 0 || shoe.Cards.Count < shoe.Size * ent.Comp.ReshuffleFraction;
    }

    private void SetPose(Entity<BlackjackTableComponent> ent, BlackjackPose pose, float seconds)
    {
        ent.Comp.Pose = pose;
        if (TryComp<ActiveBlackjackTableComponent>(ent, out var active))
            active.PoseEnd = _timing.CurTime + TimeSpan.FromSeconds(seconds);
    }

    private bool IsAbsent(BlackjackSeat seat)
    {
        return seat.Leaving || !TryGetEntity(seat.Player, out var player) || _mobState.IsDead(player.Value);
    }

    private void BeginRound(Entity<BlackjackTableComponent> ent)
    {
        var comp = ent.Comp;
        foreach (var seat in comp.Seats)
        {
            seat.Hands.Clear();
        }

        comp.DealerHand.Clear();
        comp.DealerHidden = false;
        comp.ActiveSeat = -1;
        comp.ActiveHand = 0;
        Comp<BlackjackShoeComponent>(ent).Hole = null;
        if (TryComp<ActiveBlackjackTableComponent>(ent, out var active))
            active.Deals.Clear();

        ClearCards(ent);

        for (var i = 0; i < comp.Seats.Count; i++)
        {
            var seat = comp.Seats[i];
            if (seat.Occupied && IsAbsent(seat))
                Unseat(ent, i);
        }

        var anySeated = false;
        foreach (var seat in comp.Seats)
        {
            anySeated |= seat.Occupied;
        }

        if (!anySeated)
        {
            if (comp.Phase != BlackjackPhase.Idle)
                ResetTable(ent);

            Refresh(ent);
            return;
        }

        var reshuffled = false;
        if (NeedsReshuffle(ent))
        {
            Reshuffle(ent);
            reshuffled = true;
        }

        var duration = comp.Wager ? comp.BetTime : comp.IntermissionTime;
        comp.Phase = BlackjackPhase.Betting;
        comp.PhaseDuration = duration;
        comp.PhaseEnd = _timing.CurTime + TimeSpan.FromSeconds(duration);
        Say(ent, comp.Wager ? "say-bets" : "say-next");
        comp.Face = BlackjackFace.Neutral;
        if (!reshuffled)
            comp.Pose = BlackjackPose.Idle;

        Refresh(ent);
    }

    private void EndBetting(Entity<BlackjackTableComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.Phase != BlackjackPhase.Betting)
            return;

        if (comp.Wager)
        {
            for (var i = 0; i < comp.Seats.Count; i++)
            {
                var seat = comp.Seats[i];
                if (!seat.Occupied || seat.Bet > 0)
                    continue;

                seat.Missed++;
                if (seat.Missed >= comp.MaxMissed)
                {
                    Say(ent, "say-kicked", ("name", seat.Name));
                    Unseat(ent, i);
                }
            }

            if (comp.Phase == BlackjackPhase.Idle)
                return;
        }

        var participants = new List<int>();
        var anySeated = false;
        for (var i = 0; i < comp.Seats.Count; i++)
        {
            var seat = comp.Seats[i];
            anySeated |= seat.Occupied;
            if (seat.Occupied && (!comp.Wager || seat.Bet > 0))
                participants.Add(i);
        }

        if (participants.Count == 0)
        {
            if (anySeated)
                BeginRound(ent);

            return;
        }

        if (comp.Wager)
            Say(ent, "say-bets-closed");

        var active = EnsureComp<ActiveBlackjackTableComponent>(ent);
        active.Deals.Clear();
        foreach (var index in participants)
        {
            var seat = comp.Seats[index];
            seat.Hands = new List<BlackjackHand> { new() { Bet = seat.Bet } };
            seat.Bet = 0;
        }

        foreach (var index in participants)
        {
            active.Deals.Enqueue(new BlackjackDeal(index, 0, false));
        }

        active.Deals.Enqueue(new BlackjackDeal(-1, 0, false));
        foreach (var index in participants)
        {
            active.Deals.Enqueue(new BlackjackDeal(index, 0, false));
        }

        active.Deals.Enqueue(new BlackjackDeal(-1, 0, true));

        var now = _timing.CurTime;
        comp.Phase = BlackjackPhase.Dealing;
        comp.PhaseDuration = 0f;
        comp.PhaseEnd = now;
        active.NextStep = now + TimeSpan.FromSeconds(comp.StepDelay);
        Refresh(ent);
    }

    private void UpdateDealing(Entity<BlackjackTableComponent> ent, ActiveBlackjackTableComponent active, TimeSpan now)
    {
        if (now < active.NextStep)
            return;

        if (active.Deals.Count > 0)
        {
            DealOne(ent, active.Deals.Dequeue());
            active.NextStep += TimeSpan.FromSeconds(ent.Comp.StepDelay);
            return;
        }

        FinishDealing(ent);
    }

    private void DealOne(Entity<BlackjackTableComponent> ent, BlackjackDeal deal)
    {
        var comp = ent.Comp;
        if (deal.Seat < 0)
        {
            if (deal.Hidden)
            {
                var card = Draw(ent);
                Comp<BlackjackShoeComponent>(ent).Hole = card;
                comp.DealerHidden = true;
                SpawnCard(ent, -1, 0, comp.DealerHand.Count, card, false);
                AfterDeal(ent);
            }
            else
            {
                DealerDraw(ent);
            }
        }
        else if (deal.Seat < comp.Seats.Count && deal.Hand < comp.Seats[deal.Seat].Hands.Count)
        {
            DealNow(ent, deal.Seat, comp.Seats[deal.Seat].Hands[deal.Hand], deal.Hand);
        }

        Refresh(ent);
    }

    private void DealerDraw(Entity<BlackjackTableComponent> ent)
    {
        var card = Draw(ent);
        SpawnCard(ent, -1, 0, ent.Comp.DealerHand.Count, card, true);
        ent.Comp.DealerHand.Add(card);
        AfterDeal(ent);
    }

    private void AfterDeal(Entity<BlackjackTableComponent> ent)
    {
        _audio.PlayPvs(ent.Comp.DealSound, ent);
        SetPose(ent, BlackjackPose.Deal, ent.Comp.DealPoseTime);
    }

    private void DealNow(Entity<BlackjackTableComponent> ent, int seatIndex, BlackjackHand hand, int handIndex)
    {
        var card = Draw(ent);
        SpawnCard(ent, seatIndex, handIndex, hand.Cards.Count, card, true);
        hand.Cards.Add(card);
        AfterDeal(ent);
    }

    private void FinishDealing(Entity<BlackjackTableComponent> ent)
    {
        var comp = ent.Comp;
        foreach (var seat in comp.Seats)
        {
            foreach (var hand in seat.Hands)
            {
                if (!BlackjackRules.IsBlackjack(hand))
                    continue;

                hand.State = BlackjackHandState.Blackjack;
                Say(ent, "say-blackjack", ("name", seat.Name));
            }
        }

        var hole = Comp<BlackjackShoeComponent>(ent).Hole;
        if (comp.DealerHand.Count > 0
            && hole != null
            && BlackjackRules.DealerPeeks(comp.DealerHand[0])
            && BlackjackRules.IsNatural(new[] { comp.DealerHand[0], hole.Value }))
        {
            RevealHole(ent);
            Say(ent, "say-dealer-blackjack");
            StartPayout(ent);
            return;
        }

        comp.Phase = BlackjackPhase.Turns;
        NextTurn(ent);
    }

    private void NextTurn(Entity<BlackjackTableComponent> ent)
    {
        var comp = ent.Comp;
        var firstSeat = Math.Max(comp.ActiveSeat, 0);
        var firstHand = comp.ActiveSeat >= 0 ? comp.ActiveHand : 0;
        for (var i = firstSeat; i < comp.Seats.Count; i++)
        {
            var seat = comp.Seats[i];
            for (var h = i == firstSeat ? firstHand : 0; h < seat.Hands.Count; h++)
            {
                var hand = seat.Hands[h];
                if (hand.State != BlackjackHandState.Playing)
                    continue;

                if (IsAbsent(seat))
                {
                    seat.Leaving = true;
                    hand.State = BlackjackHandState.Stood;
                    continue;
                }

                comp.ActiveSeat = i;
                comp.ActiveHand = h;
                comp.PhaseDuration = comp.TurnTime;
                comp.PhaseEnd = _timing.CurTime + TimeSpan.FromSeconds(comp.TurnTime);
                Say(ent, h == 1 ? "say-ask-split" : "say-ask", ("name", seat.Name));
                Refresh(ent);
                return;
            }
        }

        StartDealer(ent);
    }

    private void UpdateTurns(Entity<BlackjackTableComponent> ent, TimeSpan now)
    {
        var comp = ent.Comp;
        if (comp.ActiveSeat < 0 || comp.ActiveSeat >= comp.Seats.Count || comp.ActiveHand >= comp.Seats[comp.ActiveSeat].Hands.Count)
        {
            NextTurn(ent);
            return;
        }

        var seat = comp.Seats[comp.ActiveSeat];
        var hand = seat.Hands[comp.ActiveHand];
        if (hand.State != BlackjackHandState.Playing)
        {
            NextTurn(ent);
            return;
        }

        if (IsAbsent(seat))
        {
            seat.Leaving = true;
            hand.State = BlackjackHandState.Stood;
            NextTurn(ent);
            return;
        }

        if (now < comp.PhaseEnd)
            return;

        Say(ent, "say-timeout", ("name", seat.Name));
        hand.State = BlackjackHandState.Stood;
        if (!comp.Wager)
        {
            seat.Missed++;
            if (seat.Missed >= comp.MaxMissed)
                seat.Leaving = true;
        }

        NextTurn(ent);
    }

    private bool TryAction(Entity<BlackjackTableComponent> ent, int seatIndex, BlackjackAction action)
    {
        var comp = ent.Comp;
        if (comp.Phase != BlackjackPhase.Turns || seatIndex != comp.ActiveSeat || seatIndex < 0 || seatIndex >= comp.Seats.Count)
            return false;

        if (!TryComp<ActiveBlackjackTableComponent>(ent, out var active) || active.Deals.Count > 0)
            return false;

        var seat = comp.Seats[seatIndex];
        if (comp.ActiveHand >= seat.Hands.Count)
            return false;

        var handIndex = comp.ActiveHand;
        var hand = seat.Hands[handIndex];
        if (hand.State != BlackjackHandState.Playing)
            return false;

        seat.Missed = 0;
        switch (action)
        {
            case BlackjackAction.Hit:
            {
                DealNow(ent, seatIndex, hand, handIndex);
                var total = BlackjackRules.Total(hand.Cards);
                if (total > BlackjackRules.Target)
                {
                    Bust(ent, seat, hand);
                    NextTurn(ent);
                }
                else if (total == BlackjackRules.Target)
                {
                    hand.State = BlackjackHandState.Stood;
                    NextTurn(ent);
                }
                else
                {
                    comp.PhaseDuration = comp.TurnTime;
                    comp.PhaseEnd = _timing.CurTime + TimeSpan.FromSeconds(comp.TurnTime);
                    Say(ent, "say-total", ("total", total));
                }

                break;
            }
            case BlackjackAction.Stand:
                hand.State = BlackjackHandState.Stood;
                NextTurn(ent);
                break;
            case BlackjackAction.Double:
                if (!BlackjackRules.CanDouble(hand))
                {
                    Say(ent, "say-cannot-double");
                    return false;
                }

                if (comp.Wager && seat.Chips < hand.Bet)
                {
                    Say(ent, "say-no-funds");
                    return false;
                }

                seat.Chips -= hand.Bet;
                hand.Bet *= 2;
                hand.Doubled = true;
                DealNow(ent, seatIndex, hand, handIndex);
                if (BlackjackRules.Total(hand.Cards) > BlackjackRules.Target)
                    Bust(ent, seat, hand);
                else
                    hand.State = BlackjackHandState.Stood;

                NextTurn(ent);
                break;
            case BlackjackAction.Split:
                if (!BlackjackRules.CanSplit(seat, hand))
                {
                    Say(ent, "say-cannot-split");
                    return false;
                }

                if (comp.Wager && seat.Chips < hand.Bet)
                {
                    Say(ent, "say-no-funds");
                    return false;
                }

                seat.Chips -= hand.Bet;
                var second = new BlackjackHand
                {
                    Bet = hand.Bet,
                    FromSplit = true,
                    Cards = new List<BlackjackCard> { hand.Cards[1] },
                };
                hand.Cards.RemoveAt(1);
                hand.FromSplit = true;
                seat.Hands.Add(second);
                RespawnSeatCards(ent, seatIndex);
                DealNow(ent, seatIndex, hand, 0);
                DealNow(ent, seatIndex, second, 1);
                var aces = hand.Cards[0].Rank == 1;
                foreach (var split in seat.Hands)
                {
                    if (aces || BlackjackRules.Total(split.Cards) == BlackjackRules.Target)
                        split.State = BlackjackHandState.Stood;
                }

                NextTurn(ent);
                break;
        }

        Refresh(ent);
        return true;
    }

    private void Bust(Entity<BlackjackTableComponent> ent, BlackjackSeat seat, BlackjackHand hand)
    {
        hand.State = BlackjackHandState.Bust;
        Say(ent, "say-bust", ("name", seat.Name));
        ent.Comp.Face = BlackjackFace.Happy;
    }

    private void StartDealer(Entity<BlackjackTableComponent> ent)
    {
        var comp = ent.Comp;
        comp.ActiveSeat = -1;
        comp.ActiveHand = 0;
        comp.Phase = BlackjackPhase.Dealer;
        comp.PhaseDuration = 0f;
        comp.PhaseEnd = _timing.CurTime;
        if (TryComp<ActiveBlackjackTableComponent>(ent, out var active))
            active.NextStep = _timing.CurTime + TimeSpan.FromSeconds(comp.StepDelay);

        Refresh(ent);
    }

    private void UpdateDealer(Entity<BlackjackTableComponent> ent, ActiveBlackjackTableComponent active, TimeSpan now)
    {
        if (now < active.NextStep)
            return;

        var comp = ent.Comp;
        var step = TimeSpan.FromSeconds(comp.StepDelay);
        if (comp.DealerHidden)
        {
            RevealHole(ent);
            active.NextStep += step;
            Refresh(ent);
            return;
        }

        var anyStood = false;
        foreach (var seat in comp.Seats)
        {
            foreach (var hand in seat.Hands)
            {
                anyStood |= hand.State == BlackjackHandState.Stood;
            }
        }

        if (anyStood && BlackjackRules.DealerDraws(comp.DealerHand))
        {
            DealerDraw(ent);
            active.NextStep += step;
            Refresh(ent);
            return;
        }

        var total = BlackjackRules.Total(comp.DealerHand);
        if (total > BlackjackRules.Target)
            Say(ent, "say-dealer-bust");
        else
            Say(ent, "say-dealer-total", ("total", total));

        StartPayout(ent);
    }

    private static int ResultRank(BlackjackResult result)
    {
        return result switch
        {
            BlackjackResult.Blackjack => 4,
            BlackjackResult.Win => 3,
            BlackjackResult.Push => 2,
            BlackjackResult.Lose => 1,
            _ => 0,
        };
    }

    private void StartPayout(Entity<BlackjackTableComponent> ent)
    {
        var comp = ent.Comp;
        var anyWin = false;
        var anyPush = false;
        var anySettled = false;
        for (var i = 0; i < comp.Seats.Count; i++)
        {
            var seat = comp.Seats[i];
            if (!seat.Occupied || seat.Hands.Count == 0)
                continue;

            anySettled = true;
            var best = BlackjackResult.None;
            foreach (var hand in seat.Hands)
            {
                hand.Result = BlackjackRules.Settle(hand, comp.DealerHand, out var payout);
                hand.Payout = payout;
                seat.Chips = Math.Min(seat.Chips + payout, comp.MaxChips);
                if (ResultRank(hand.Result) > ResultRank(best))
                    best = hand.Result;

                if (comp.Wager && payout > 0)
                    LogPayout(ent, seat, payout);
            }

            switch (best)
            {
                case BlackjackResult.Win:
                case BlackjackResult.Blackjack:
                    anyWin = true;
                    if (!comp.Wager)
                        comp.Wins[seat.Name] = comp.Wins.GetValueOrDefault(seat.Name) + 1;

                    Say(ent, "say-win", ("name", seat.Name));
                    break;
                case BlackjackResult.Push:
                    anyPush = true;
                    Say(ent, "say-push", ("name", seat.Name));
                    break;
                default:
                    Say(ent, "say-lose", ("name", seat.Name));
                    break;
            }
        }

        comp.ActiveSeat = -1;
        comp.Phase = BlackjackPhase.Payout;
        comp.Face = anyWin ? BlackjackFace.Sad : anySettled && !anyPush ? BlackjackFace.Happy : BlackjackFace.Neutral;
        if (anyWin)
            _audio.PlayPvs(comp.WinSound, ent);

        comp.PhaseDuration = comp.PayoutTime;
        comp.PhaseEnd = _timing.CurTime + TimeSpan.FromSeconds(comp.PayoutTime);
        Refresh(ent);
    }

    private void LogPayout(Entity<BlackjackTableComponent> ent, BlackjackSeat seat, int payout)
    {
        if (TryGetEntity(seat.Player, out var player))
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player.Value):player} получил {payout} фишек за столом {ToPrettyString(ent):target}");
        else
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{seat.Name} получил {payout} фишек за столом {ToPrettyString(ent):target}");
    }
}
