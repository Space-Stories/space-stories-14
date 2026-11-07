using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Events;

public sealed partial class StoriesHereticOpenKnowledgeEvent : InstantActionEvent;

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

public sealed partial class StoriesHereticKnitFleshEvent : EntityTargetActionEvent;

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

    [DataField]
    public StoriesHereticFlameEffects Flames = new() { Count = 24 };

    [DataField]
    public float FlameMinDistance = 0.5f;
}

public sealed partial class StoriesHereticVoidBlastEvent : InstantActionEvent
{
    [DataField]
    public float Range = 3f;

    [DataField]
    public float HalfAngleDegrees = 35f;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public int ChillStacks = 2;

    [DataField]
    public int IceRows = 3;

    [DataField]
    public EntProtoId Ice = "STHereticVoidIce";

    [DataField]
    public StoriesHereticFlameEffects Frost = new()
    {
        Effect = "STHereticVoidFrostEffect",
        Count = 20,
        MinScale = 0.3f,
        MaxScale = 0.6f,
        MinLifetime = 0.5f,
        MaxLifetime = 1f,
        MaxRotation = Angle.FromDegrees(180),
    };
}

public sealed partial class StoriesHereticVoidPhaseEvent : WorldTargetActionEvent
{
    [DataField]
    public float MinRange = 3f;

    [DataField]
    public float MaxRange = 9f;

    [DataField]
    public float Radius = 1f;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public int ChillStacks = 1;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Stories/Heretic/voidblink.ogg");

    [DataField]
    public EntProtoId EffectIn = "STHereticVoidPhaseIn";

    [DataField]
    public EntProtoId EffectOut = "STHereticVoidPhaseOut";

    [DataField]
    public LocId Speech = "stories-heretic-void-phase-speech";
}

public sealed partial class StoriesHereticVoidPullEvent : InstantActionEvent
{
    [DataField]
    public float DamageRadius = 1f;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float KnockdownRadius = 4f;

    [DataField]
    public TimeSpan Knockdown = TimeSpan.FromSeconds(3);

    [DataField]
    public float PullRadius = 7f;

    [DataField]
    public float PullDistance = 3f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Stories/Heretic/voidblink.ogg");

    [DataField]
    public EntProtoId Effect = "STHereticVoidPhaseIn";

    [DataField]
    public LocId Speech = "stories-heretic-void-pull-speech";
}

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

public sealed partial class StoriesHereticMansusLinkEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(6);

    [DataField]
    public float Range = 7f;
}

public sealed partial class StoriesHereticExpandSightEvent : InstantActionEvent
{
    [DataField]
    public float Zoom = 1.5f;
}

public sealed partial class StoriesHereticEldritchEmpEvent : InstantActionEvent
{
    [DataField]
    public float Range = 6f;

    [DataField]
    public float Energy = 50000f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);
}

public sealed partial class StoriesHereticStalkerShapeshiftEvent : InstantActionEvent;

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

    [DataField]
    public LocId Speech = "stories-heretic-shed-form-speech";
}

public sealed partial class StoriesHereticBloodSiphonEvent : EntityTargetActionEvent
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public FixedPoint2 Heal = 15;

    [DataField]
    public FixedPoint2 Blood = 15;

    [DataField]
    public float BleedChance = 0.5f;

    [DataField]
    public float BleedShare = 0.5f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}

public sealed partial class StoriesHereticBloodCleaveEvent : EntityTargetActionEvent
{
    [DataField]
    public float Radius = 1f;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float Bleed = 5f;

    [DataField]
    public EntProtoId Effect = "STHereticCleaveEffect";

    [DataField]
    public EntProtoId Puddle = "PuddleBlood";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Stories/Heretic/slice.ogg");
}

public sealed partial class StoriesHereticVoidPrisonEvent : EntityTargetActionEvent
{
    [DataField]
    public EntProtoId Prison = "STHereticVoidPrison";

    [DataField]
    public LocId Invalid = "stories-heretic-bridge-prison-invalid";
}
