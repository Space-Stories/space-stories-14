using Robust.Shared.Serialization;

namespace Content.Shared._Stories.ERT;

[Serializable, NetSerializable]
public enum ERTSpawnConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ERTSpawnConsoleState : BoundUserInterfaceState
{
    public List<ERTSpawnOption> Options;
    public bool IsOnCooldown;
    public int ActiveOptionIndex;
    public int Balance;
    public bool HasAdminAccess;
    public List<bool> CanCall;
    public LocId TitleName;
    public LocId BalanceName;

    public ERTSpawnConsoleState(
        List<ERTSpawnOption> options,
        bool isOnCooldown,
        int activeOptionIndex,
        int balance,
        bool hasAdminAccess,
        List<bool> canCall,
        LocId titleName,
        LocId balanceName)
    {
        Options = options;
        IsOnCooldown = isOnCooldown;
        ActiveOptionIndex = activeOptionIndex;
        Balance = balance;
        HasAdminAccess = hasAdminAccess;
        CanCall = canCall;
        TitleName = titleName;
        BalanceName = balanceName;
    }
}

[Serializable, NetSerializable]
public sealed class ERTSpawnConsoleCallMessage : BoundUserInterfaceMessage
{
    public int OptionIndex;

    public ERTSpawnConsoleCallMessage(int optionIndex)
    {
        OptionIndex = optionIndex;
    }
}