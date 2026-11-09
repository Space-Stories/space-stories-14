namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticBladeBleedComponent : Component
{
    [DataField]
    public float Bleed = 4f;
}
