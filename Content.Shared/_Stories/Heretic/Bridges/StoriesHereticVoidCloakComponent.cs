using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Bridges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(StoriesHereticVoidCloakSystem))]
public sealed partial class StoriesHereticVoidCloakComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Hidden;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticVoidCloakHoodComponent : Component;
