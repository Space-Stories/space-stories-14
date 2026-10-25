namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticRawProphetComponent : Component
{
    [DataField]
    public EntityUid? Master;

    [DataField]
    public HashSet<EntityUid> Linked = new();

    [DataField]
    public TimeSpan BreakParalyze = TimeSpan.FromSeconds(0.5);

    public bool SightExpanded;
}
