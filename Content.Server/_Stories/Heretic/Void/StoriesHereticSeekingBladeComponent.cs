using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Void;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticVoidSystem))]
public sealed partial class StoriesHereticSeekingBladeComponent : Component
{
    [DataField]
    public float Range = 9f;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUse;
}
