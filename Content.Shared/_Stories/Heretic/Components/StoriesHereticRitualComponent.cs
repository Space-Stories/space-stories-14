using Content.Shared.Body;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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
    public bool AscendedUnlimited;

    [DataField]
    public StoriesHereticRitualEffect Effect = StoriesHereticRitualEffect.None;
}

[DataDefinition]
public sealed partial class StoriesHereticIngredient
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public List<EntProtoId>? Prototypes;

    [DataField]
    public ProtoId<OrganCategoryPrototype>? OrganCategory;

    [DataField]
    public List<ProtoId<ReagentPrototype>>? Reagents;

    [DataField]
    public FixedPoint2 ReagentAmount = 10;

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
    Summon,
    Ghoul,
}
