using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Summons;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticSummonSystem))]
public sealed partial class StoriesHereticSummonComponent : Component
{
    [DataField]
    public EntityUid? Master;

    [DataField]
    public List<EntProtoId> Ingredients = new();

    [DataField]
    public List<ProtoId<StackPrototype>> Stacks = new();

    [DataField]
    public List<StoriesHereticSummonLimb> Limbs = new();

    [DataField]
    public TimeSpan ClaimTime = TimeSpan.FromMinutes(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ExpireTime;

    [DataField]
    public bool Bound;

    public bool Collapsed;
}

[DataDefinition]
public sealed partial class StoriesHereticSummonLimb
{
    [DataField]
    public EntProtoId Body;

    [DataField]
    public List<EntProtoId> Organs = new();

    [DataField]
    public List<int> Parents = new();
}
