using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Events;

public sealed partial class StoriesHereticOpenKnowledgeEvent : InstantActionEvent;

public sealed partial class StoriesHereticDrawRuneEvent : WorldTargetActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField]
    public float CodexSpeedMultiplier = 3f;

    [DataField]
    public EntProtoId Rune = "STHereticRune";
}

public sealed partial class StoriesHereticGraspEvent : EntityTargetActionEvent
{
    [DataField]
    public float StaminaDamage = 80f;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan SlurTime = TimeSpan.FromSeconds(5);
}

/// <summary>Сшить плоть: касание лечит слугу еретика.</summary>
public sealed partial class StoriesHereticKnitFleshEvent : EntityTargetActionEvent;

/// <summary>Ослепление Сырого пророка: цель слепнет на <see cref="Duration"/>.</summary>
public sealed partial class StoriesHereticEldritchBlindEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);
}

public sealed partial class StoriesHereticLivingHeartEvent : InstantActionEvent
{
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [DataField]
    public float NearDistance = 15f;

    [DataField]
    public float MediumDistance = 40f;
}

public sealed partial class StoriesHereticVolcanoBlastEvent : InstantActionEvent
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public int Bounces = 5;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float FireStacks = 3f;

    [DataField]
    public DamageSpecifier SelfHeal = new();
}

public sealed partial class StoriesHereticRebirthEvent : InstantActionEvent
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public DamageSpecifier HealPerTarget = new();

    [DataField]
    public DamageSpecifier FinishDamage = new();
}

public sealed partial class StoriesHereticFireRingEvent : InstantActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);
}

public sealed partial class StoriesHereticFireWaveEvent : InstantActionEvent
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public float HalfAngleDegrees = 30f;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float FireStacks = 5f;

    /// <summary>
    /// Языки пламени в случайных точках конуса (только визуал).
    /// </summary>
    [DataField]
    public StoriesHereticFlameEffects Flames = new() { Count = 24 };

    /// <summary>
    /// Ближайшее к еретику расстояние, на котором появляются языки пламени.
    /// </summary>
    [DataField]
    public float FlameMinDistance = 0.5f;
}

/// <summary>
/// Параметры россыпи маленьких языков пламени: сколько, какого размера и сколько живут.
/// Только визуал, ничего не поджигает.
/// </summary>
[DataDefinition]
public sealed partial class StoriesHereticFlameEffects
{
    [DataField]
    public EntProtoId Effect = "STHereticFireEffect";

    [DataField]
    public int Count = 8;

    [DataField]
    public float MinScale = 0.9f;

    [DataField]
    public float MaxScale = 1.3f;

    [DataField]
    public float MinLifetime = 0.4f;

    [DataField]
    public float MaxLifetime = 0.8f;

    /// <summary>
    /// Наибольшее отклонение поворота языка пламени в обе стороны.
    /// </summary>
    [DataField]
    public Angle MaxRotation = Angle.FromDegrees(25);
}

public sealed partial class StoriesHereticShadowCloakEvent : InstantActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(3);

    [DataField]
    public TimeSpan MinCooldown = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan InterruptCooldown = TimeSpan.FromMinutes(3);

    [DataField]
    public float SpeedMultiplier = 1.25f;

    [DataField]
    public float DamageThreshold = 25f;

    [DataField]
    public TimeSpan InterruptKnockdown = TimeSpan.FromSeconds(0.5);

    [DataField]
    public float InterruptSlowdown = 0.5f;

    [DataField]
    public TimeSpan InterruptSlowTime = TimeSpan.FromMinutes(2);

    [DataField]
    public EntProtoId Smoke = "STHereticShadowCloakSmoke";
}
