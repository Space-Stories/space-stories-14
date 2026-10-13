using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Вешается на сущность действия: действие не срабатывает без фокуса.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticRequiresFocusComponent : Component;
