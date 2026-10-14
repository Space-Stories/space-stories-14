using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Summons;

/// <summary>
/// Призванный монстр Мансуса. Пока роль не занята, ждёт игрока; при смерти роняет ингредиенты рецепта (Paradise).
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticSummonSystem))]
public sealed partial class StoriesHereticSummonComponent : Component
{
    [DataField]
    public EntityUid? Master;

    /// <summary>
    /// Прототипы поглощённых ингредиентов — вернутся на пол, если роль не займут или моб умрёт.
    /// Рецепты призывов не используют трупы: прототип тела вернулся бы пустым мобом.
    /// </summary>
    [DataField]
    public List<EntProtoId> Ingredients = new();

    [DataField]
    public TimeSpan ClaimTime = TimeSpan.FromMinutes(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ExpireTime;

    /// <summary>
    /// Роль занята игроком.
    /// </summary>
    [DataField]
    public bool Bound;
}
