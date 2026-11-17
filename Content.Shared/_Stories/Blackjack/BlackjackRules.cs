namespace Content.Shared._Stories.Blackjack;

public static class BlackjackRules
{
    public const int Target = 21;
    public const int DealerStand = 17;

    public static int FindSeat(IReadOnlyList<BlackjackSeat> seats, NetEntity player)
    {
        if (!player.IsValid())
            return -1;

        for (var i = 0; i < seats.Count; i++)
        {
            if (seats[i].Player == player)
                return i;
        }

        return -1;
    }

    public static int CardValue(BlackjackCard card)
    {
        return card.Rank >= 10 ? 10 : card.Rank;
    }

    public static int Total(IReadOnlyList<BlackjackCard> cards, out bool soft)
    {
        var total = 0;
        var aces = 0;
        foreach (var card in cards)
        {
            total += CardValue(card);
            if (card.Rank == 1)
                aces++;
        }

        soft = aces > 0 && total + 10 <= Target;
        return soft ? total + 10 : total;
    }

    public static int Total(IReadOnlyList<BlackjackCard> cards)
    {
        return Total(cards, out _);
    }

    public static bool IsNatural(IReadOnlyList<BlackjackCard> cards)
    {
        return cards.Count == 2 && Total(cards) == Target;
    }

    public static bool IsBlackjack(BlackjackHand hand)
    {
        return !hand.FromSplit && IsNatural(hand.Cards);
    }

    public static bool CanSplit(BlackjackSeat seat, BlackjackHand hand)
    {
        return seat.Hands.Count == 1
               && hand.State == BlackjackHandState.Playing
               && hand.Cards.Count == 2
               && hand.Cards[0].Rank == hand.Cards[1].Rank;
    }

    public static bool CanDouble(BlackjackHand hand)
    {
        return hand.State == BlackjackHandState.Playing && hand.Cards.Count == 2 && !hand.Doubled;
    }

    public static bool DealerDraws(IReadOnlyList<BlackjackCard> dealer)
    {
        return Total(dealer) < DealerStand;
    }

    public static bool DealerPeeks(BlackjackCard upCard)
    {
        return upCard.Rank == 1 || upCard.Rank >= 10;
    }

    public static BlackjackResult Settle(BlackjackHand hand, IReadOnlyList<BlackjackCard> dealer, out int payout)
    {
        payout = 0;
        var total = Total(hand.Cards);
        if (total > Target)
            return BlackjackResult.Lose;

        var natural = IsBlackjack(hand);
        var dealerNatural = IsNatural(dealer);

        if (natural && dealerNatural)
        {
            payout = hand.Bet;
            return BlackjackResult.Push;
        }

        if (natural)
        {
            payout = hand.Bet + (hand.Bet * 3 + 1) / 2;
            return BlackjackResult.Blackjack;
        }

        if (dealerNatural)
            return BlackjackResult.Lose;

        var dealerTotal = Total(dealer);
        if (dealerTotal > Target || total > dealerTotal)
        {
            payout = hand.Bet * 2;
            return BlackjackResult.Win;
        }

        if (total == dealerTotal)
        {
            payout = hand.Bet;
            return BlackjackResult.Push;
        }

        return BlackjackResult.Lose;
    }
}
