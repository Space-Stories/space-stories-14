using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Void;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticVoidSystem))]
public sealed partial class StoriesHereticVoidStormComponent : Component
{
    [DataField]
    public float Radius = 7f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;

    [DataField]
    public TimeSpan MuteTime = TimeSpan.FromSeconds(4);

    [DataField]
    public int ChillStacks = 1;

    [DataField]
    public DamageSpecifier Damage = new() { DamageDict = { ["Cold"] = 2 } };

    [DataField]
    public EntityUid? MusicStream;

    [DataField]
    public StoriesHereticFlameEffects Snowflakes = new()
    {
        Effect = "STHereticVoidSnowflake",
        Count = 6,
        MinScale = 0.3f,
        MaxScale = 0.5f,
        MinLifetime = 0.5f,
        MaxLifetime = 1f
    };

    [DataField]
    public float LightRadius = 4f;

    [DataField]
    public float LightEnergy = 0.4f;

    [DataField]
    public Color LightColor = Color.FromHex("#9fd8ff");

    public bool LightAdded;
}
