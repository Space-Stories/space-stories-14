using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Stories.Heretic.Void;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(StoriesHereticVoidChillSystem))]
public sealed partial class StoriesHereticVoidChillComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Stacks;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick;

    [DataField, AutoNetworkedField]
    public int MaxStacks = 5;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public float CoolingPerStack = 4f;

    [DataField]
    public float MinTemperature = 230f;

    [DataField]
    public float SlowPerStack = 0.1f;
}
