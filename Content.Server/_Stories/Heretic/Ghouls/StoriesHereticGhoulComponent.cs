using Content.Shared.FixedPoint;

namespace Content.Server._Stories.Heretic.Ghouls;

/// <summary>
/// Выдаётся прототипом обращения гуля. Хрупкость гуля по tg: доля обычного здоровья (25/50/125 из 100).
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticGhoulComponent : Component
{
    [DataField(required: true)]
    public FixedPoint2 CritThreshold;

    [DataField(required: true)]
    public FixedPoint2 DeadThreshold;

    [DataField]
    public FixedPoint2? OriginalCrit;

    [DataField]
    public FixedPoint2? OriginalDead;
}
