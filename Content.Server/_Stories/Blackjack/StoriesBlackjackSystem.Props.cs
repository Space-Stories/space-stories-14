using System.Numerics;
using Content.Shared._Stories.Blackjack;
using Robust.Shared.Map;

namespace Content.Server._Stories.Blackjack;

public sealed partial class StoriesBlackjackSystem
{
    private Vector2 CardSpot(BlackjackTableComponent comp, int seat, int hand, int index)
    {
        if (seat < 0)
            return comp.DealerSpot + comp.DealerStep * index;

        var shift = hand == 1 ? comp.SplitShifts[seat] : Vector2.Zero;
        return comp.CardSpots[seat] + shift + comp.CardSteps[seat] * index;
    }

    private void SpawnCard(Entity<BlackjackTableComponent> ent, int seat, int hand, int index, BlackjackCard card, bool faceUp, bool slide = true)
    {
        var comp = ent.Comp;
        var target = CardSpot(comp, seat, hand, index);
        var uid = Spawn(comp.CardProto, new EntityCoordinates(ent, slide ? comp.ShoeSpot : target));
        var cardComp = EnsureComp<BlackjackTableCardComponent>(uid);
        cardComp.Card = faceUp ? card : default;
        cardComp.FaceUp = faceUp;
        cardComp.Order = index;
        Dirty(uid, cardComp);

        if (slide)
        {
            var slideComp = EnsureComp<BlackjackSlideComponent>(uid);
            slideComp.Target = target;
            slideComp.Speed = comp.SlideSpeed;
        }

        var props = EnsureComp<BlackjackPropsComponent>(ent);
        props.Cards.Add((uid, seat, hand));
        if (!faceUp)
            props.HoleCard = uid;
    }

    private void RevealHole(Entity<BlackjackTableComponent> ent)
    {
        var shoe = Comp<BlackjackShoeComponent>(ent);
        if (shoe.Hole is not { } hole)
            return;

        shoe.Hole = null;
        ent.Comp.DealerHand.Add(hole);
        ent.Comp.DealerHidden = false;

        var props = Comp<BlackjackPropsComponent>(ent);
        if (props.HoleCard is { } uid && TryComp<BlackjackTableCardComponent>(uid, out var cardComp))
        {
            cardComp.Card = hole;
            cardComp.FaceUp = true;
            Dirty(uid, cardComp);
        }

        props.HoleCard = null;
    }

    private void RespawnSeatCards(Entity<BlackjackTableComponent> ent, int seatIndex)
    {
        var props = Comp<BlackjackPropsComponent>(ent);
        for (var i = props.Cards.Count - 1; i >= 0; i--)
        {
            if (props.Cards[i].Seat != seatIndex)
                continue;

            QueueDel(props.Cards[i].Uid);
            props.Cards.RemoveAt(i);
        }

        var seat = ent.Comp.Seats[seatIndex];
        for (var h = 0; h < seat.Hands.Count; h++)
        {
            var cards = seat.Hands[h].Cards;
            for (var c = 0; c < cards.Count; c++)
            {
                SpawnCard(ent, seatIndex, h, c, cards[c], true, false);
            }
        }
    }

    private void ClearCards(Entity<BlackjackTableComponent> ent)
    {
        if (!TryComp<BlackjackPropsComponent>(ent, out var props))
            return;

        foreach (var (uid, _, _) in props.Cards)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            var slide = EnsureComp<BlackjackSlideComponent>(uid);
            slide.Target = ent.Comp.ShoeSpot;
            slide.Speed = ent.Comp.SlideSpeed;
            slide.Delete = true;
        }

        props.Cards.Clear();
        props.HoleCard = null;
    }

    private void DeleteProps(Entity<BlackjackTableComponent> ent)
    {
        if (!TryComp<BlackjackPropsComponent>(ent, out var props))
            return;

        foreach (var (uid, _, _) in props.Cards)
        {
            QueueDel(uid);
        }

        props.Cards.Clear();
        props.HoleCard = null;
        for (var i = 0; i < props.BetPiles.Length; i++)
        {
            QueueDel(props.BetPiles[i]);
            props.BetPiles[i] = null;
        }

        for (var i = 0; i < props.StackPiles.Length; i++)
        {
            QueueDel(props.StackPiles[i]);
            props.StackPiles[i] = null;
        }
    }

    private void SyncPiles(Entity<BlackjackTableComponent> ent)
    {
        var comp = ent.Comp;
        if (!comp.Wager || !TryComp<BlackjackPropsComponent>(ent, out var props))
            return;

        var count = Math.Min(comp.Seats.Count, Math.Min(props.BetPiles.Length, props.StackPiles.Length));
        for (var i = 0; i < count; i++)
        {
            var seat = comp.Seats[i];
            SyncPile(ent, ref props.BetPiles[i], AtStake(seat), comp.BetSpots[i]);
            SyncPile(ent, ref props.StackPiles[i], seat.Chips, comp.StackSpots[i]);
        }
    }

    private void SyncPile(Entity<BlackjackTableComponent> ent, ref EntityUid? slot, int amount, Vector2 spot)
    {
        if (slot != null && TerminatingOrDeleted(slot.Value))
            slot = null;

        if (amount <= 0)
        {
            if (slot is { } old)
                QueueDel(old);

            slot = null;
            return;
        }

        if (slot is not { } uid)
        {
            uid = Spawn(ent.Comp.PileProto, new EntityCoordinates(ent, spot));
            slot = uid;
        }

        var pile = EnsureComp<BlackjackChipPileComponent>(uid);
        if (pile.Amount == amount)
            return;

        pile.Amount = amount;
        Dirty(uid, pile);
    }

    private void UpdateSlides(float frameTime)
    {
        var query = EntityQueryEnumerator<BlackjackSlideComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var slide, out var xform))
        {
            var delta = slide.Target - xform.LocalPosition;
            var distance = delta.Length();
            var step = slide.Speed * frameTime;
            if (distance <= step)
            {
                _transform.SetLocalPosition(uid, slide.Target, xform);
                if (slide.Delete)
                    QueueDel(uid);
                else
                    RemCompDeferred<BlackjackSlideComponent>(uid);
            }
            else
            {
                _transform.SetLocalPosition(uid, xform.LocalPosition + delta / distance * step, xform);
            }
        }
    }
}
