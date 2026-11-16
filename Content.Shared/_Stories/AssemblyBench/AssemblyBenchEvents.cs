using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.AssemblyBench;

[Serializable, NetSerializable]
public enum AssemblyBenchUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class AssemblyBenchApplyMessage(int step) : BoundUserInterfaceMessage
{
    public readonly int Step = step;
}

[Serializable, NetSerializable]
public sealed partial class AssemblyBenchDoAfterEvent : DoAfterEvent
{
    [DataField]
    public int Step;

    private AssemblyBenchDoAfterEvent()
    {
    }

    public AssemblyBenchDoAfterEvent(int step)
    {
        Step = step;
    }

    public override DoAfterEvent Clone() => this;
}
