using Content.Shared.Body;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticArmEaterComponent : Component
{
    [DataField]
    public float HealFraction = 0.5f;

    [DataField]
    public List<ProtoId<OrganCategoryPrototype>> Categories = new() { "ArmLeft", "ArmRight", "HandLeft", "HandRight" };

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");
}
