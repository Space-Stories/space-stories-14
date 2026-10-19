using Content.Shared.FixedPoint;

namespace Content.Server._Stories.Heretic.Ghouls;

[RegisterComponent, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticRaisedComponent : Component
{
    [DataField]
    public FixedPoint2? OriginalCrit;

    [DataField]
    public FixedPoint2? OriginalDead;
}
