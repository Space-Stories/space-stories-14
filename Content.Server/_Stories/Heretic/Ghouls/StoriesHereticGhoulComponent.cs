using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ghouls;

[RegisterComponent, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticGhoulComponent : Component
{
    [DataField(required: true)]
    public FixedPoint2 CritThreshold;

    [DataField(required: true)]
    public FixedPoint2 DeadThreshold;

    [DataField]
    public EntProtoId? StatusEffect;
}
