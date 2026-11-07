namespace Content.Server._Stories.Heretic.Ash;

[RegisterComponent, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticAshMarkComponent : Component
{
    [DataField]
    public float StaminaDamage = 20f;

    [DataField]
    public float BaseFireStacks = 2f;

    [DataField]
    public float SpreadRadius = 2f;

    [DataField]
    public int MaxHops = 3;

    [DataField]
    public TimeSpan SpreadMarkDuration = TimeSpan.FromSeconds(15);
}
