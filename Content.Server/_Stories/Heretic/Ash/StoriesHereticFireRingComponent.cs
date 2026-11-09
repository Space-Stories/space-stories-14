using Content.Shared._Stories.Heretic.Events;
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

    [DataField]
    public EntProtoId RingEffect = "STHereticFireRingEffect";

    [DataField]
    public float RingSpriteRadius = 1f;

    [DataField]
    public StoriesHereticFlameEffects Flames = new() { Count = 5 };

    [DataField]
    public float FlameSpread = 0.3f;

    [DataField]
    public EntityUid? RingEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
