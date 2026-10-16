using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// На еретике: Хватка ослепляет.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticGraspBlindComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);
}
