using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticInfluenceComponent : Component
{
    [DataField]
    public TimeSpan HarvestTime = TimeSpan.FromSeconds(5);

    [DataField]
    public int Points = 1;

    [DataField]
    public EntProtoId PiercedPrototype = "STHereticPiercedReality";

    [DataField]
    public SoundSpecifier? HarvestSound = new SoundPathSpecifier("/Audio/Magic/staff_animation.ogg");
}
