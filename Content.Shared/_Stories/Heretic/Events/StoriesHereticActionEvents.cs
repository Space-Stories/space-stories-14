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
    /// Визуальный эффект пламени, расставляемый по конусу волны.
    /// </summary>
    [DataField]
    public EntProtoId Effect = "STHereticFireEffect";

    [DataField]
    public int EffectsPerStep = 5;
}
