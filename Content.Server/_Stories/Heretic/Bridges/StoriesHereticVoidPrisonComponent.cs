using Content.Shared._Stories.Heretic.Bridges;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Bridges;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticBridgeSystem))]
public sealed partial class StoriesHereticVoidPrisonComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan OpeningTime = TimeSpan.FromSeconds(1.4);

    [DataField]
    public TimeSpan ClosingTime = TimeSpan.FromSeconds(1);

    [DataField]
    public int ChillStacks = 1;

    [DataField]
    public string ContainerId = "void_prison";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ActiveTime;

    [DataField]
    public EntityUid? Prisoner;

    [DataField]
    public StoriesHereticVoidPrisonState State = StoriesHereticVoidPrisonState.Opening;
}
