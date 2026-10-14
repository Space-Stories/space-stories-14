using Content.Shared.Body;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Рецепт ритуала. Живёт только на прототипах <c>STHereticRitual*</c>.
/// </summary>
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

    /// <summary>
    /// Лужа на руне, в которой не меньше <see cref="ReagentAmount"/> любого из реагентов (кровь, пепел, рвота).
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>>? Reagents;

    [DataField]
    public FixedPoint2 ReagentAmount = 10;

    [DataField]
    public int Amount = 1;

    [DataField]
    public bool Consume = true;

    /// <summary>
    /// Название для сообщения «не хватает ...».
    /// </summary>
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
