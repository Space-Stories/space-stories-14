namespace Content.Server._Stories.Heretic.Ash;

[RegisterComponent, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticBladeFireComponent : Component
{
    [DataField]
    public float FireStacks = 1f;
}
