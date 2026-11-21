using Content.Shared.Stacks;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared._Stories.AssemblyBench;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(StoriesAssemblyBenchSystem))]
public sealed partial class AssemblyBenchComponent : Component
{
    [DataField(required: true)]
    public List<AssemblyBenchStep> Steps = new();

    [DataField, AutoNetworkedField]
    public int Step;

    [DataField, AutoNetworkedField]
    public int Completed;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan WorkEnd;

    [DataField, AutoNetworkedField]
    public float WorkDuration;

    [DataField(required: true)]
    public EntProtoId Result;

    [DataField(required: true)]
    public ResPath Sprite;

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    [DataField]
    public SoundSpecifier? FinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}

[DataDefinition]
public sealed partial class AssemblyBenchStep
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public EntProtoId Icon;

    [DataField]
    public ProtoId<StackPrototype>? Stack;

    [DataField]
    public int Amount = 1;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public ProtoId<ToolQualityPrototype>? Tool;

    [DataField]
    public float Delay = 3f;

    [DataField]
    public float Fuel;
}
