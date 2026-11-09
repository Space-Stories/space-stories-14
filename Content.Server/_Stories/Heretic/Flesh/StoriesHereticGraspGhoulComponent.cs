using Content.Shared._Stories.Conversion;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticGraspGhoulComponent : Component
{
    [DataField]
    public ProtoId<ConversionPrototype> Conversion = "STHereticGhoul";

    [DataField]
    public int Limit = 3;

    [DataField]
    public int AscendedBonus = 3;
}
