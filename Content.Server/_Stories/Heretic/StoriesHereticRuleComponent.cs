using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Stories.Heretic;

[RegisterComponent, Access(typeof(StoriesHereticRuleSystem))]
public sealed partial class StoriesHereticRuleComponent : Component
{
    [DataField]
    public int BaseInfluences = 8;

    [DataField]
    public int InfluencesPerHeretic = 2;

    [DataField]
    public EntProtoId InfluencePrototype = "STHereticInfluence";

    [DataField]
    public bool InfluencesSpawned;

    /// <summary>
    /// Готовая карта Теневого мира от маппера. Если не задана — генерируется в коде.
    /// </summary>
    [DataField]
    public ResPath? ShadowRealmMap;
}
