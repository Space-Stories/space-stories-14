using Content.Shared._Stories.Conversion;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ghouls;

[RegisterComponent, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticGhoulRitualComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ConversionPrototype> Conversion;

    [DataField(required: true)]
    public int Limit;

    [DataField]
    public int AscendedBonus;

    [DataField]
    public TimeSpan OfferTime = TimeSpan.FromMinutes(5);

    [DataField]
    public EntProtoId Refund = "FoodPoppy";
}
