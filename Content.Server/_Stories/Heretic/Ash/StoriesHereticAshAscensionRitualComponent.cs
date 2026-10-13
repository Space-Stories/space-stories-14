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
    /// Аура вознёсшегося, прикрепляемая к еретику (как heretic_aura в tg).
    /// </summary>
    [DataField]
    public EntProtoId? Aura = "STHereticAscensionAura";
}
