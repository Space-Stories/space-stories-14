using Content.Shared.Dataset;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Состояние еретика. Изменяется только через API <see cref="StoriesHereticSystem"/>.
/// </summary>
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

    /// <summary>
    /// Число купленных знаний (без стартовых и Пира сов) — для цели «изучить N знаний».
    /// </summary>
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

    /// <summary>
    /// Призванные монстры, чью роль занял игрок. Гули не считаются (Paradise).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SummonCount;

    /// <summary>
    /// Игроки, уже засчитанные в <see cref="SummonCount"/>.
    /// </summary>
    [DataField]
    public HashSet<NetUserId> SummonedPlayers = new();

    [DataField, AutoNetworkedField]
    public int RequiredSummons;

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

    /// <summary>
    /// Всего получено очков знаний за раунд (без стартовых). Траты не уменьшают.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PointsGained;

    /// <summary>
    /// Аура появляется, когда <see cref="PointsGained"/> превышает это значение (points_to_aura в tg).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PointsToAura = 8;

    /// <summary>
    /// Видимая всем аура еретика (heretic_aura в tg). Скрыта после Пира сов.
    /// </summary>
    [DataField]
    public EntProtoId Aura = "STHereticAura";

    [DataField]
    public EntityUid? AuraEntity;

    /// <summary>
    /// Аура скрыта одеждой (Обугленная мантия), как TRAIT_HERETIC_AURA_HIDDEN в tg.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AuraHidden;
}
