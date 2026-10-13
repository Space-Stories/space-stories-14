namespace Content.Server._Stories.Heretic.Ash;

/// <summary>
/// На еретике: удары клинком еретика поджигают.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticBladeFireComponent : Component
{
    [DataField]
    public float FireStacks = 1f;
}
