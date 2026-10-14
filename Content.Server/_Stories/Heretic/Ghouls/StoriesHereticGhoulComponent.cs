using Content.Shared.FixedPoint;

namespace Content.Server._Stories.Heretic.Ghouls;

/// <summary>
/// Выдаётся прототипом обращения гуля. Хрупкость гуля по tg: доля обычного здоровья (25/50/125 из 100).
/// Обычные пороги хранит <see cref="StoriesHereticRaisedComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticGhoulComponent : Component
{
    [DataField(required: true)]
    public FixedPoint2 CritThreshold;

    [DataField(required: true)]
    public FixedPoint2 DeadThreshold;

    /// <summary>
    /// Безгласный мертвец не может говорить.
    /// </summary>
    [DataField]
    public bool Mute;
}
