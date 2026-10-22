namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticFocusedAttackComponent : Component
{
    [DataField]
    public float Bonus = 2;

    [DataField]
    public float MaxBonus = 10;

    [ViewVariables]
    public EntityUid? LastTarget;

    [ViewVariables]
    public int Streak;
}
