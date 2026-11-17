using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Blackjack;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BlackjackTableCardComponent : Component
{
    [AutoNetworkedField]
    public BlackjackCard Card;

    [AutoNetworkedField]
    public bool FaceUp;

    [AutoNetworkedField]
    public int Order;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BlackjackChipPileComponent : Component
{
    [AutoNetworkedField]
    public int Amount;

    [DataField]
    public List<int> Thresholds = new() { 1, 5, 25, 100, 500, 2500 };
}
