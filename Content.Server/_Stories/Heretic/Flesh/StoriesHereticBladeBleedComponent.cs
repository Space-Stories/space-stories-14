namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// На еретике: удары клинком еретика вызывают кровотечение.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticBladeBleedComponent : Component
{
    [DataField]
    public float Bleed = 4f;
}
