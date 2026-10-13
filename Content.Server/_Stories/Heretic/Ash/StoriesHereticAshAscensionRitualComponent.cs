using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ash;

/// <summary>
/// Данные ритуала Вознесения Ash. Живёт на прототипе ритуала.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticAshAscensionRitualComponent : Component
{
    [DataField]
    public int Corpses = 3;

    [DataField]
    public FixedPoint2 HeatThreshold = 100;

    [DataField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public ProtoId<DamageModifierSetPrototype> DamageModifierSet = "STHereticAshAscended";

    [DataField]
    public LocId Announcement = "stories-heretic-ash-ascension-announcement";

    [DataField]
    public Color AnnouncementColor = Color.OrangeRed;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Ambience/Antag/ling_alert.ogg");

    /// <summary>
    /// Во сколько раз умножить число скачков «Извержения вулкана» (как ash_final в tg).
    /// </summary>
    [DataField]
    public float VolcanoBounceMultiplier = 2f;

    /// <summary>
    /// Множители перезарядки уже имеющихся действий по ID их прототипа.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, float> CooldownMultipliers = new();

    /// <summary>
    /// Компоненты, выдаваемые вознёсшемуся (иммунитет к взрывам и т. п.).
    /// </summary>
    [DataField]
    public ComponentRegistry AddComponents = new();

    /// <summary>
    /// Вознёсшемуся больше не нужно дышать (TRAIT_NOBREATH в tg; как у зомби — снимается дыхание).
    /// </summary>
    [DataField]
    public bool RemoveBreathing = true;
}
