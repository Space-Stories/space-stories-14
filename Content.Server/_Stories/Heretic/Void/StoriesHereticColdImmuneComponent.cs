namespace Content.Server._Stories.Heretic.Void;

[RegisterComponent, Access(typeof(StoriesHereticVoidSystem))]
public sealed partial class StoriesHereticColdImmuneComponent : Component
{
    [DataField]
    public float? OriginalThreshold;

    [DataField]
    public bool RemoveBreathing = true;
}
