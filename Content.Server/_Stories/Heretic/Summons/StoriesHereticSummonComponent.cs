using Content.Shared.Stacks;
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

    /// <summary>
    /// Типы поглощённых стопок: ритуал забирает одну единицу, её же и возвращаем (иначе Spawn по прототипу дал бы стопку целиком).
    /// Лужи не возвращаются вовсе.
    /// </summary>
    [DataField]
    public List<ProtoId<StackPrototype>> Stacks = new();

    /// <summary>
    /// Поглощённые отделённые части тела: пустой DetachedBody потерял бы орган, поэтому помним и содержимое.
    /// </summary>
    [DataField]
    public List<StoriesHereticSummonLimb> Limbs = new();

    [DataField]
    public TimeSpan ClaimTime = TimeSpan.FromMinutes(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ExpireTime;

    /// <summary>
    /// Роль занята игроком.
    /// </summary>
    [DataField]
    public bool Bound;

    /// <summary>
    /// Схлопывание уже произошло, повторно не срабатывать.
    /// </summary>
    public bool Collapsed;
}

/// <summary>
/// Отделённая часть тела: прототип тела, прототипы органов и индекс родителя каждого органа (-1 — корень).
/// </summary>
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
