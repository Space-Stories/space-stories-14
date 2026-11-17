using Content.Shared._Stories.Blackjack;
using Robust.Client.GameObjects;

namespace Content.Client._Stories.Blackjack;

public sealed partial class StoriesBlackjackVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public event Action<EntityUid>? TableStateChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlackjackTableComponent, ComponentStartup>(OnTableStartup);
        SubscribeLocalEvent<BlackjackTableComponent, AfterAutoHandleStateEvent>(OnTableState);
        SubscribeLocalEvent<BlackjackTableCardComponent, ComponentStartup>(OnCardStartup);
        SubscribeLocalEvent<BlackjackTableCardComponent, AfterAutoHandleStateEvent>(OnCardState);
        SubscribeLocalEvent<BlackjackChipPileComponent, ComponentStartup>(OnPileStartup);
        SubscribeLocalEvent<BlackjackChipPileComponent, AfterAutoHandleStateEvent>(OnPileState);
    }

    private void OnTableStartup(Entity<BlackjackTableComponent> ent, ref ComponentStartup args)
    {
        UpdateTable(ent);
    }

    private void OnTableState(Entity<BlackjackTableComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            UpdateTable(ent);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update blackjack table visuals for {ToPrettyString(ent)}: {e}");
        }

        try
        {
            TableStateChanged?.Invoke(ent.Owner);
        }
        catch (Exception e)
        {
            Log.Error($"Blackjack table state subscriber failed for {ToPrettyString(ent)}: {e}");
        }
    }

    private void OnCardStartup(Entity<BlackjackTableCardComponent> ent, ref ComponentStartup args)
    {
        UpdateCard(ent);
    }

    private void OnCardState(Entity<BlackjackTableCardComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            UpdateCard(ent);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update blackjack card visuals for {ToPrettyString(ent)}: {e}");
        }
    }

    private void OnPileStartup(Entity<BlackjackChipPileComponent> ent, ref ComponentStartup args)
    {
        UpdatePile(ent);
    }

    private void OnPileState(Entity<BlackjackChipPileComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            UpdatePile(ent);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update blackjack chip pile visuals for {ToPrettyString(ent)}: {e}");
        }
    }

    private void UpdateTable(Entity<BlackjackTableComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var target = new Entity<SpriteComponent?>(ent.Owner, sprite);
        var comp = ent.Comp;

        SetState(target, "robot", comp.Pose switch
        {
            BlackjackPose.Deal => "robot-deal",
            BlackjackPose.Shuffle => "robot-shuffle",
            _ => "robot-idle",
        });

        SetState(target, "face", comp.Face switch
        {
            BlackjackFace.Happy => "face-happy",
            BlackjackFace.Sad => "face-sad",
            _ => "face-neutral",
        });

        for (var i = 0; i < comp.SeatSpots.Count; i++)
        {
            var visible = comp.Phase == BlackjackPhase.Turns && comp.ActiveSeat == i;
            if (_sprite.LayerMapTryGet(target, $"lamp{i}", out var index, false))
                _sprite.LayerSetVisible(target, index, visible);
        }
    }

    private void UpdateCard(Entity<BlackjackTableCardComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var card = ent.Comp.Card;
        SetState((ent.Owner, sprite), "card", ent.Comp.FaceUp ? $"{card.Suit}{card.Rank}" : "Back");
        sprite.RenderOrder = (uint) ent.Comp.Order;
    }

    private void UpdatePile(Entity<BlackjackChipPileComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var best = 0;
        foreach (var threshold in ent.Comp.Thresholds)
        {
            if (threshold <= ent.Comp.Amount && threshold > best)
                best = threshold;
        }

        if (best == 0)
            return;

        SetState((ent.Owner, sprite), "pile", $"chips-{best}");
    }

    private void SetState(Entity<SpriteComponent?> sprite, string key, string state)
    {
        if (_sprite.TryGetLayer(sprite, key, out var layer, false))
            _sprite.LayerSetRsiState(layer, state);
    }
}
