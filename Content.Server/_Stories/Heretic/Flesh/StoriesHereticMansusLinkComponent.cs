namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// На цели Связи Мансуса: к какому пророку она привязана.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticMansusLinkComponent : Component
{
    [DataField]
    public EntityUid Prophet;
}
