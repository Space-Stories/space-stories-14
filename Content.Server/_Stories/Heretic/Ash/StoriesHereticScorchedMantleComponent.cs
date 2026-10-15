using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Ash;

/// <summary>
/// Обугленная мантия: еретик в ней не гаснет и может поджигать себя, остальным она сжигает конечности.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticScorchedMantleComponent : Component
{
    [DataField]
    public TimeSpan FlameInterval = TimeSpan.FromSeconds(5);

    [DataField]
    public float FlameStacks = 1f;

    /// <summary>
    /// Раз в столько не-еретик теряет одну конечность.
    /// </summary>
    [DataField]
    public TimeSpan BurnInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public List<ProtoId<OrganCategoryPrototype>> BurnLimbs = new() { "ArmLeft", "ArmRight", "LegLeft", "LegRight" };

    /// <summary>
    /// Что остаётся на месте сгоревшей конечности.
    /// </summary>
    [DataField]
    public EntProtoId LimbAsh = "Ash";

    [DataField]
    public EntityUid? Wearer;

    [DataField]
    public bool Flames;

    /// <summary>
    /// Исходная скорость затухания огня носителя, вернётся при снятии.
    /// </summary>
    [DataField]
    public float? SavedFirestackFade;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
