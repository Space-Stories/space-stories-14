using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Blackjack;

[Serializable, NetSerializable]
public enum BlackjackUiKey : byte
{
    Lobby,
    Game,
}

[Serializable, NetSerializable]
public sealed class BlackjackSitMessage(int seat) : BoundUserInterfaceMessage
{
    public readonly int Seat = seat;
}

[Serializable, NetSerializable]
public sealed class BlackjackLeaveMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class BlackjackReadyMessage(bool ready) : BoundUserInterfaceMessage
{
    public readonly bool Ready = ready;
}

[Serializable, NetSerializable]
public sealed class BlackjackSpectateMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class BlackjackCashierMessage(BlackjackCashierOp op, int amount) : BoundUserInterfaceMessage
{
    public readonly BlackjackCashierOp Op = op;
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class BlackjackBetMessage(int amount) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class BlackjackActionMessage(BlackjackAction action, int hand) : BoundUserInterfaceMessage
{
    public readonly BlackjackAction Action = action;
    public readonly int Hand = hand;
}
