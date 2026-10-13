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
    /// Сколько раз пробовать найти безопасную плитку станции.
    /// </summary>
    [DataField]
    public int TileAttempts = 30;

    /// <summary>
    /// Минимальное парциальное давление кислорода, кПа (safe_oxygen_min в tg).
    /// </summary>
    [DataField]
    public float MinOxygenPressure = 16f;

    /// <summary>
    /// Готовая карта Теневого мира от маппера. Если не задана — генерируется в коде.
    /// </summary>
    [DataField]
    public ResPath? ShadowRealmMap;
}
