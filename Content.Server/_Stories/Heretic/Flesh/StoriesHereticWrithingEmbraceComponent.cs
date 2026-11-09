using Content.Shared.Damage;
using Content.Shared.EntityEffects.Effects.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticWrithingEmbraceComponent : Component
{
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [DataField]
    public float Range = 15f;

    [DataField]
    public DistributedHealthChange Heal = new() { Damage = new() { ["Brute"] = -3, ["Burn"] = -3, ["Airloss"] = -3 } };

    [DataField]
    public DamageSpecifier Wound = new() { DamageDict = new() { ["Slash"] = 10 } };

    [DataField]
    public float WoundBleed = 3f;

    [DataField]
    public int Wounds = 6;

    [DataField]
    public int WoundsLeft;

    [DataField]
    public EntityUid? Wearer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
