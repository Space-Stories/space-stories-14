using System.Numerics;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Stories.Blackjack;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class BlackjackTableComponent : Component
{
    [DataField]
    public bool Wager = true;

    [DataField]
    public int MinBet = 5;

    [DataField]
    public int MaxBet;

    [DataField]
    public int MaxChips = 1000000;

    [DataField]
    public float ReturnRange = 4.5f;

    [DataField]
    public int Decks = 4;

    [DataField]
    public float ReshuffleFraction = 0.25f;

    [DataField]
    public float BetTime = 20f;

    [DataField]
    public float IntermissionTime = 5f;

    [DataField]
    public float TurnTime = 20f;

    [DataField]
    public float ReadyTime = 60f;

    [DataField]
    public float StepDelay = 0.7f;

    [DataField]
    public float ShufflePoseTime = 2.1f;

    [DataField]
    public float DealPoseTime = 0.35f;

    [DataField]
    public float PayoutTime = 6f;

    [DataField]
    public int MaxMissed = 2;

    [DataField]
    public float SlideSpeed = 4f;

    [DataField(required: true)]
    public List<Vector2> SeatSpots = new();

    [DataField(required: true)]
    public List<Vector2> CardSpots = new();

    [DataField(required: true)]
    public List<Vector2> CardSteps = new();

    [DataField(required: true)]
    public List<Vector2> SplitShifts = new();

    [DataField(required: true)]
    public List<Vector2> BetSpots = new();

    [DataField(required: true)]
    public List<Vector2> StackSpots = new();

    [DataField]
    public Vector2 DealerSpot = new(-0.2f, 0.26f);

    [DataField]
    public Vector2 DealerStep = new(0.12f, 0f);

    [DataField]
    public Vector2 ShoeSpot = new(0.62f, 0.3f);

    [DataField]
    public EntProtoId CardProto = "STBlackjackTableCard";

    [DataField]
    public EntProtoId PileProto = "STBlackjackChipPile";

    [DataField]
    public EntProtoId ChipsProto = "STCasinoChips";

    [DataField]
    public EntProtoId CashProto = "SpaceCash";

    [DataField]
    public ProtoId<StackPrototype> ChipStack = "STCasinoChip";

    [DataField]
    public ProtoId<StackPrototype> CashStack = "Credit";

    [DataField(required: true)]
    public ResPath CardSprite;

    [DataField(required: true)]
    public ResPath LobbySprite;

    [DataField]
    public SoundSpecifier? DealSound = new SoundCollectionSpecifier("STAddCard");

    [DataField]
    public SoundSpecifier? ShuffleSound = new SoundCollectionSpecifier("STShuffleDeck");

    [DataField]
    public SoundSpecifier? ChipSound = new SoundPathSpecifier("/Audio/Items/Dice/coin1.ogg");

    [DataField]
    public SoundSpecifier? WinSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");

    [AutoNetworkedField]
    public BlackjackPhase Phase;

    [AutoNetworkedField]
    public List<BlackjackSeat> Seats = new();

    [AutoNetworkedField]
    public List<BlackjackCard> DealerHand = new();

    [AutoNetworkedField]
    public bool DealerHidden;

    [AutoNetworkedField]
    public int ActiveSeat = -1;

    [AutoNetworkedField]
    public int ActiveHand;

    [AutoNetworkedField, AutoPausedField]
    public TimeSpan PhaseEnd;

    [AutoNetworkedField]
    public float PhaseDuration;

    [AutoNetworkedField]
    public BlackjackPose Pose;

    [AutoNetworkedField]
    public BlackjackFace Face;

    [AutoNetworkedField]
    public Dictionary<string, int> Wins = new();
}
