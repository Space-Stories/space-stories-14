using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent]
public sealed partial class StoriesHereticRitualComponent : Component
{
    [DataField]
    public List<StoriesHereticIngredient> Ingredients = new();

    [DataField]
    public List<EntProtoId> Results = new();

    [DataField]
    public int? Limit;

    [DataField]
    public StoriesHereticRitualEffect Effect = StoriesHereticRitualEffect.None;
}

[DataDefinition]
public sealed partial class StoriesHereticIngredient
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public List<EntProtoId>? Prototypes;

    [DataField]
    public ProtoId<OrganCategoryPrototype>? OrganCategory;

    [DataField]
    public SlotFlags? ClothingSlots;

    [DataField]
    public int Amount = 1;

    [DataField]
    public bool Consume = true;

    [DataField(required: true)]
    public LocId Name;
}

public enum StoriesHereticRitualEffect : byte
{
    None,
    Sacrifice,
    Knowledge,
    RerollTargets,
    Ascension,
}
