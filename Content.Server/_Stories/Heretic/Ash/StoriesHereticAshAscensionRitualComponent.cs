using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ash;

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

    [DataField]
    public float VolcanoBounceMultiplier = 2f;

    [DataField]
    public Dictionary<EntProtoId, float> CooldownMultipliers = new();

    [DataField]
    public ComponentRegistry AddComponents = new();

    [DataField]
    public bool RemoveBreathing = true;
}
