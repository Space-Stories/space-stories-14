using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Ash;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticScorchedMantleComponent : Component
{
    [DataField]
    public TimeSpan FlameInterval = TimeSpan.FromSeconds(5);

    [DataField]
    public float FlameStacks = 1f;

    [DataField]
    public TimeSpan BurnInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public List<ProtoId<OrganCategoryPrototype>> BurnLimbs = new() { "ArmLeft", "ArmRight", "LegLeft", "LegRight" };

    [DataField]
    public EntProtoId LimbAsh = "Ash";

    [DataField]
    public EntityUid? Wearer;

    [DataField]
    public bool Flames;

    [DataField]
    public float? SavedFirestackFade;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
