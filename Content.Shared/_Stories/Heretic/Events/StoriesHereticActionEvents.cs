using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Events;

public sealed partial class StoriesHereticOpenKnowledgeEvent : InstantActionEvent;

/// <summary>
/// Переключает пламя Обугленной мантии. Поднимается на мантии.
/// </summary>
public sealed partial class StoriesHereticMantleFlamesEvent : InstantActionEvent;

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

/// <summary>Связь Мансуса: запускает DoAfter привязки цели к каналу пророка.</summary>
public sealed partial class StoriesHereticMansusLinkEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(6);

    /// <summary>Дальше этого расстояния DoAfter прерывается.</summary>
    [DataField]
    public float Range = 7f;
}

/// <summary>Расширение взора: переключает увеличение обзора камеры.</summary>
public sealed partial class StoriesHereticExpandSightEvent : InstantActionEvent
{
    [DataField]
    public float Zoom = 1.5f;
}

/// <summary>ЭМИ-импульс Сталкера вокруг себя.</summary>
public sealed partial class StoriesHereticEldritchEmpEvent : InstantActionEvent
{
    [DataField]
    public float Range = 6f;

    [DataField]
    public float Energy = 50000f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);
}

/// <summary>Смена облика Сталкера: первый раз открывает меню выбора, потом превращает в выбранный облик.</summary>
public sealed partial class StoriesHereticStalkerShapeshiftEvent : InstantActionEvent;

/// <summary>
/// «Сбросить форму» вознёсшегося на Пути Плоти: превращение во Владыку Ночи и ужас свидетелей вокруг.
/// Обратно — штатным действием возврата полиморфа.
/// </summary>
public sealed partial class StoriesHereticShedFormEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph = "STHereticLordOfNight";

    [DataField]
    public float FearRange = 9f;

    [DataField]
    public float FearChance = 0.25f;

    [DataField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan StutterTime = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan DizzyTime = TimeSpan.FromSeconds(30);

    [DataField]
    public EntProtoId StutterEffect = "StatusEffectStutter";

    [DataField]
    public EntProtoId DizzyEffect = "StatusEffectWoozy";

    [DataField]
    public LocId FearMessage = "stories-heretic-lord-fear";

    /// <summary>
    /// Крик перед превращением: произносится обработчиком, пока еретик ещё на станции.
    /// </summary>
    [DataField]
    public LocId Speech = "stories-heretic-shed-form-speech";
}
