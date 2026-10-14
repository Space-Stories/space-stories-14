using Content.Shared.FixedPoint;

namespace Content.Server._Stories.Heretic.Ghouls;

/// <summary>
/// Труп уже служил гулем — поднять его второй раз нельзя (Paradise: «once per corpse»).
/// Хранит обычные пороги тела: мёртвому бывшему гулю их вернут при оживлении.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticRaisedComponent : Component
{
    [DataField]
    public FixedPoint2? OriginalCrit;

    [DataField]
    public FixedPoint2? OriginalDead;
}
