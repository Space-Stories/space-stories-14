using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StoriesHereticSystem))]
public sealed partial class StoriesHereticCodexComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Open;

    [DataField]
    public int InfluenceMultiplier = 2;
}
