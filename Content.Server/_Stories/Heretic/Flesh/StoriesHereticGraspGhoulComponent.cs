using Content.Shared._Stories.Conversion;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// На еретике: Хватка Мансуса поднимает мёртвых гуманоидов гулями.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticGraspGhoulComponent : Component
{
    [DataField]
    public ProtoId<ConversionPrototype> Conversion = "STHereticGhoul";

    [DataField]
    public int Limit = 3;

    /// <summary>
    /// Добавка к лимиту после Вознесения.
    /// </summary>
    [DataField]
    public int AscendedBonus = 3;
}
