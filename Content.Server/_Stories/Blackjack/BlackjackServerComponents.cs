using System.Numerics;
using Content.Shared._Stories.Blackjack;

namespace Content.Server._Stories.Blackjack;

[RegisterComponent, Access(typeof(StoriesBlackjackSystem))]
public sealed partial class BlackjackShoeComponent : Component
{
    public List<BlackjackCard> Cards = new();
    public BlackjackCard? Hole;
    public int Size;
}

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesBlackjackSystem))]
public sealed partial class ActiveBlackjackTableComponent : Component
{
    [AutoPausedField]
    public TimeSpan NextStep;

    [AutoPausedField]
    public TimeSpan PoseEnd;

    public Queue<BlackjackDeal> Deals = new();
}

[RegisterComponent, Access(typeof(StoriesBlackjackSystem))]
public sealed partial class BlackjackPlayerComponent : Component
{
    public EntityUid Table;
}

[RegisterComponent, Access(typeof(StoriesBlackjackSystem))]
public sealed partial class BlackjackPropsComponent : Component
{
    public List<(EntityUid Uid, int Seat, int Hand)> Cards = new();
    public EntityUid? HoleCard;
    public EntityUid?[] BetPiles = new EntityUid?[0];
    public EntityUid?[] StackPiles = new EntityUid?[0];
}

[RegisterComponent, Access(typeof(StoriesBlackjackSystem))]
public sealed partial class BlackjackSlideComponent : Component
{
    public Vector2 Target;
    public float Speed;
    public bool Delete;
}

public readonly record struct BlackjackDeal(int Seat, int Hand, bool Hidden);
