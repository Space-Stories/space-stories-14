using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Слуга еретика: призванный монстр или гуль. Проверяется через <see cref="StoriesHereticSystem.IsServant"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StoriesHereticSystem))]
public sealed partial class StoriesHereticServantComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Master;
}
