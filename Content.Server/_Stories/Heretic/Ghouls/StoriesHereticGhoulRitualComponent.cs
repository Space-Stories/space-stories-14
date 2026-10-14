using Content.Shared._Stories.Conversion;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ghouls;

/// <summary>
/// На прототипе ритуала с эффектом Ghoul: кого и в каком количестве поднимать с руны.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticGhoulRitualComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ConversionPrototype> Conversion;

    [DataField(required: true)]
    public int Limit;

    /// <summary>
    /// Добавка к лимиту после Вознесения.
    /// </summary>
    [DataField]
    public int AscendedBonus;

    /// <summary>
    /// Сколько призрачная роль ждёт игрока на трупе без души.
    /// </summary>
    [DataField]
    public TimeSpan OfferTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Возвращается на место трупа, если роль никто не занял (спецификация §6.3).
    /// </summary>
    [DataField]
    public EntProtoId Refund = "FoodPoppy";
}
