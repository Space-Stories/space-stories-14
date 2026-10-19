using Content.Shared.Dataset;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(StoriesHereticSystem))]
public sealed partial class StoriesHereticComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField, AutoNetworkedField]
    public int Points = 1;

    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype>? Path;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> Knowledge = new();

    [DataField, AutoNetworkedField]
    public int LearnedCount;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> Rituals = new();

    [DataField, AutoNetworkedField]
    public List<NetEntity> Targets = new();

    [DataField, AutoNetworkedField]
    public int TrackedTarget;

    [DataField, AutoNetworkedField]
    public int SacrificeCount;

    [DataField, AutoNetworkedField]
    public int HeadSacrificeCount;

    [DataField, AutoNetworkedField]
    public int RequiredKnowledge = 6;

    [DataField, AutoNetworkedField]
    public int RequiredSacrifices = 4;

    [DataField, AutoNetworkedField]
    public int RequiredHeadSacrifices = 1;

    [DataField]
    public int TargetCount = 5;

    [DataField, AutoNetworkedField]
    public bool Ascended;

    [DataField, AutoNetworkedField]
    public bool CanAscend = true;

    [DataField, AutoNetworkedField]
    public bool KnowledgeRitualUsed;

    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> KnowledgeRitualTags = new();

    [DataField]
    public ProtoId<DatasetPrototype> KnowledgeRitualDataset = "STHereticKnowledgeRitualTags";

    [DataField]
    public int KnowledgeRitualTagCount = 4;

    [DataField]
    public int KnowledgeRitualPoints = 4;

    [DataField]
    public int SacrificePoints = 2;

    [DataField]
    public int HeadSacrificePoints = 3;

    [DataField]
    public TimeSpan PassivePointInterval = TimeSpan.FromMinutes(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPassivePoint;

    [DataField]
    public Dictionary<EntProtoId, List<EntityUid>> LimitedResults = new();

    [DataField]
    public List<EntityUid> GrantedActions = new();

    [DataField]
    public EntProtoId KnowledgeAction = "STActionHereticKnowledge";

    [DataField, AutoNetworkedField]
    public int PointsGained;

    [DataField, AutoNetworkedField]
    public int PointsToAura = 8;

    [DataField]
    public EntProtoId Aura = "STHereticAura";

    [DataField]
    public EntityUid? AuraEntity;

    [DataField, AutoNetworkedField]
    public bool AuraHidden;
}
