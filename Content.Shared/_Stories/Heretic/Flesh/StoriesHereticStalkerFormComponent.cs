using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Heretic.Flesh;

/// <summary>
/// На Сталкере: список доступных обликов и выбранный облик (выбирается один раз).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StoriesHereticStalkerFormComponent : Component
{
    /// <summary>Выбранный облик; пока не выбран, действие открывает радиальное меню.</summary>
    [DataField, AutoNetworkedField]
    public ProtoId<PolymorphPrototype>? Chosen;

    [DataField(required: true)]
    public List<StoriesHereticStalkerFormOption> Forms = new();
}

/// <summary>Вариант облика: полиморф и моб, чей спрайт показывается в меню.</summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class StoriesHereticStalkerFormOption
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;

    [DataField(required: true)]
    public EntProtoId Mob;

    [DataField(required: true)]
    public LocId Name;
}

[Serializable, NetSerializable]
public enum StoriesHereticStalkerFormUiKey : byte
{
    Key,
}

/// <summary>Игрок выбрал облик в радиальном меню.</summary>
[Serializable, NetSerializable]
public sealed class StoriesHereticStalkerFormSelectMessage(ProtoId<PolymorphPrototype> polymorph) : BoundUserInterfaceMessage
{
    public readonly ProtoId<PolymorphPrototype> Polymorph = polymorph;
}
