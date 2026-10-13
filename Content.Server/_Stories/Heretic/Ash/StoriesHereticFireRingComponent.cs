using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Ash;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticFireRingComponent : Component
{
    [DataField]
    public float Radius = 2f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public float FireStacks = 2f;

    /// <summary>
    /// Визуальный эффект пламени, расставляемый по окружности кольца на каждом импульсе.
    /// </summary>
    [DataField]
    public EntProtoId Effect = "STHereticFireEffect";

    [DataField]
    public int EffectCount = 12;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
