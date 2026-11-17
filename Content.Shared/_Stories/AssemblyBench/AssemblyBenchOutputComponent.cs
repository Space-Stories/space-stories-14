using Robust.Shared.Map;

namespace Content.Shared._Stories.AssemblyBench;

[RegisterComponent]
[Access(typeof(StoriesAssemblyBenchSystem))]
public sealed partial class AssemblyBenchOutputComponent : Component
{
    [DataField]
    public EntityCoordinates Target;

    [DataField]
    public EntityCoordinates Storage;

    [DataField]
    public float Speed;

    [DataField]
    public float Range;
}
