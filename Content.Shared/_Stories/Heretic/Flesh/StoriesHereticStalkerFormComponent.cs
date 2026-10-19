using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Heretic.Flesh;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StoriesHereticStalkerFormComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<PolymorphPrototype>? Chosen;

    [DataField(required: true)]
    public List<StoriesHereticStalkerFormOption> Forms = new();
}

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

[Serializable, NetSerializable]
public sealed class StoriesHereticStalkerFormSelectMessage(ProtoId<PolymorphPrototype> polymorph) : BoundUserInterfaceMessage
{
    public readonly ProtoId<PolymorphPrototype> Polymorph = polymorph;
}
