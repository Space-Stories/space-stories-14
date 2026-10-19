using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Ash;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticMadnessMaskComponent : Component
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(2);

    [DataField]
    public float StaminaDamage = 5f;

    [DataField]
    public TimeSpan HallucinationTime = TimeSpan.FromSeconds(4);

    [DataField]
    public EntityUid? Wearer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
