using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StoriesHereticSystem))]
public sealed partial class StoriesHereticServantComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Master;
}
