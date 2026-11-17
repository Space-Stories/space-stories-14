using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Blackjack;

[Serializable, NetSerializable]
public enum BlackjackSuit : byte
{
    Clubs,
    Diamonds,
    Hearts,
    Spades,
}

[Serializable, NetSerializable]
public enum BlackjackPhase : byte
{
    Idle,
    Lobby,
    Betting,
    Dealing,
    Turns,
    Dealer,
    Payout,
}

[Serializable, NetSerializable]
public enum BlackjackHandState : byte
{
    Playing,
    Stood,
    Bust,
    Blackjack,
}

[Serializable, NetSerializable]
public enum BlackjackResult : byte
{
    None,
    Win,
    Blackjack,
    Push,
    Lose,
}

[Serializable, NetSerializable]
public enum BlackjackAction : byte
{
    Hit,
    Stand,
    Double,
    Split,
}

[Serializable, NetSerializable]
public enum BlackjackPose : byte
{
    Idle,
    Deal,
    Shuffle,
}

[Serializable, NetSerializable]
public enum BlackjackFace : byte
{
    Neutral,
    Happy,
    Sad,
}

[Serializable, NetSerializable]
public enum BlackjackCashierOp : byte
{
    BuyCash,
    BuyCard,
    Deposit,
    CashOut,
    CashOutCard,
    Withdraw,
}

[Serializable, NetSerializable]
public readonly record struct BlackjackCard(BlackjackSuit Suit, byte Rank);

[Serializable, NetSerializable]
public sealed class BlackjackHand
{
    public List<BlackjackCard> Cards = new();
    public int Bet;
    public bool Doubled;
    public bool FromSplit;
    public BlackjackHandState State;
    public BlackjackResult Result;
    public int Payout;
}

[Serializable, NetSerializable]
public sealed class BlackjackSeat
{
    public NetEntity Player;
    public string Name = string.Empty;
    public bool Ready;
    public bool Leaving;
    public int Chips;
    public int Bet;
    public int LastBet;
    public int Missed;
    public List<BlackjackHand> Hands = new();

    public bool Occupied => Player.IsValid();
}
