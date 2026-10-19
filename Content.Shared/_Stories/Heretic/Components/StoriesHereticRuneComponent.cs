using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticRuneComponent : Component
{
    [DataField]
    public float Radius = 1.5f;
}
