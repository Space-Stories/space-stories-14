using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticRuneComponent : Component
{
    /// <summary>
    /// Радиус сбора ингредиентов (руна 3×3).
    /// </summary>
    [DataField]
    public float Radius = 1.5f;
}
