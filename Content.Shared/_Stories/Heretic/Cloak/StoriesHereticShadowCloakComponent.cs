using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Stories.Heretic.Cloak;

/// <summary>
/// Еретик укрыт Плащом Тени: личность и голос скрыты, шаги беззвучны, скорость выше (tg shadow_cloak).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(StoriesHereticShadowCloakSystem))]
public sealed partial class StoriesHereticShadowCloakComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1.25f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField]
    public float DamageTaken;

    [DataField]
    public float DamageThreshold = 25f;

    [DataField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? Smoke;

    [DataField, AutoNetworkedField]
    public bool HadIdentityBlocker;

    [DataField, AutoNetworkedField]
    public bool HadFootstepTag;

    [DataField]
    public TimeSpan MinCooldown;

    [DataField]
    public TimeSpan InterruptCooldown;

    [DataField]
    public TimeSpan InterruptKnockdown;

    [DataField]
    public float InterruptSlowdown;

    [DataField]
    public TimeSpan InterruptSlowTime;
}
