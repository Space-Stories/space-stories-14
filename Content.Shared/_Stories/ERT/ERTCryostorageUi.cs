using Robust.Shared.Serialization;

namespace Content.Shared._Stories.ERT;

[NetSerializable, Serializable]
public enum ERTCryostorageUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ERTCryostorageState : BoundUserInterfaceState
{
    public List<ERTCryostorageOption> Options;
    public LocId TitleName;
    public bool HasAdminAccess;
    public bool Blocked;

    public ERTCryostorageState(
        List<ERTCryostorageOption> options,
        LocId titleName,
        bool hasAdminAccess,
        bool blocked)
    {
        Options = options;
        TitleName = titleName;
        HasAdminAccess = hasAdminAccess;
        Blocked = blocked;
    }
}

[Serializable, NetSerializable]
public sealed class ERTCryostorageSpawnMessage : BoundUserInterfaceMessage
{
    public int OptionIndex;

    public ERTCryostorageSpawnMessage(int optionIndex)
    {
        OptionIndex = optionIndex;
    }
}
