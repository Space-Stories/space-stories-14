namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// На мобе: каждый следующий удар подряд по той же цели бьёт сильнее; смена цели сбрасывает бонус.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticFocusedAttackComponent : Component
{
    /// <summary>Прибавка урона (Blunt) за каждый удар подряд по одной цели.</summary>
    [DataField]
    public float Bonus = 2;

    [DataField]
    public float MaxBonus = 10;

    [ViewVariables]
    public EntityUid? LastTarget;

    [ViewVariables]
    public int Streak;
}
