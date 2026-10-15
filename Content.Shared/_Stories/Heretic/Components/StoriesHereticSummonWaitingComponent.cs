using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Призыв ждёт игрока: неподвижен, не атакует и не взаимодействует, на клиенте полупрозрачен.
/// Снимается, когда в тело входит разум.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticSummonWaitingComponent : Component
{
    /// <summary>
    /// Только на клиенте: цвет спрайта до полупрозрачности.
    /// </summary>
    [ViewVariables]
    public Color? OriginalColor;
}
