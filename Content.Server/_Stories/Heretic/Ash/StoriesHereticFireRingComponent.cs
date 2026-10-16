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

    /// <summary>
    /// Огненное кольцо, прикреплённое к еретику на всё время действия. Только визуал.
    /// </summary>
    [DataField]
    public EntProtoId RingEffect = "STHereticFireRingEffect";

    /// <summary>
    /// Радиус кольца на спрайте при масштабе 1, в тайлах. Масштаб спрайта = <see cref="Radius"/> / это значение.
    /// </summary>
    [DataField]
    public float RingSpriteRadius = 1f;

    /// <summary>
    /// Языки пламени в случайных точках окружности на каждом импульсе.
    /// </summary>
    [DataField]
    public StoriesHereticFlameEffects Flames = new() { Count = 5 };

    /// <summary>
    /// Разброс языков пламени поперёк окружности, в тайлах.
    /// </summary>
    [DataField]
    public float FlameSpread = 0.3f;

    [DataField]
    public EntityUid? RingEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
