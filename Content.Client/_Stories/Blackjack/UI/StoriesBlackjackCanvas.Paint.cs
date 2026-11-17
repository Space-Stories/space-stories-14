using System.Numerics;
using Content.Shared._Stories.Blackjack;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;

namespace Content.Client._Stories.Blackjack.UI;

public sealed partial class StoriesBlackjackCanvas
{
    private const float RingSize = 28f;

    private readonly List<int> _others = new();

    private float Sx(float value)
    {
        return MathF.Round(value * _u);
    }

    private static float EaseOut(float t)
    {
        var r = 1f - Math.Clamp(t, 0f, 1f);
        return 1f - r * r * r;
    }

    private static UIBox2 Grow(UIBox2 box, float d)
    {
        return new UIBox2(box.Left - d, box.Top - d, box.Right + d, box.Bottom + d);
    }

    private Texture CardTexture(string name)
    {
        if (_cards != null && _cards.TryGetState(name, out var state))
            return state.GetFrame(RsiDirection.South, 0);

        return Texture.White;
    }

    private Texture Back()
    {
        return _back ??= CardTexture("Back");
    }

    private Texture Face(BlackjackCard card)
    {
        if (_faces.TryGetValue(card, out var texture))
            return texture;

        texture = CardTexture($"{card.Suit}{card.Rank}");
        _faces[card] = texture;
        return texture;
    }

    private float TextWidth(Font font, string text)
    {
        var width = 0f;
        foreach (var rune in text.EnumerateRunes())
        {
            var metrics = font.GetCharMetrics(rune, _u);
            if (metrics != null)
                width += metrics.Value.Advance;
        }

        return width;
    }

    private void DrawText(DrawingHandleScreen handle, Font font, string text, float x, float y, Color color)
    {
        var position = new Vector2(MathF.Round(x), MathF.Round(y));
        handle.DrawString(font, position + new Vector2(_k, _k), text, _u, StoriesBlackjackPalette.Shade.WithAlpha(0.75f));
        handle.DrawString(font, position, text, _u, color);
    }

    private void DrawCentred(DrawingHandleScreen handle, Font font, string text, float cx, float y, Color color)
    {
        DrawText(handle, font, text, cx - TextWidth(font, text) / 2f, y, color);
    }

    private string FitText(Font font, string text, float maxWidth)
    {
        if (TextWidth(font, text) <= maxWidth)
            return text;

        var length = Math.Min(text.Length, NameLimit);
        while (length > 1)
        {
            length--;
            var cut = text[..length] + "…";
            if (TextWidth(font, cut) <= maxWidth)
                return cut;
        }

        return string.Empty;
    }

    private float Fit(int count, float stepLogical, int scale, float maxWidth, out float step)
    {
        var cardWidth = CardWidth * scale * _k;
        step = MathF.Round(stepLogical * _k);
        if (count <= 0)
            return 0f;

        if (count == 1)
            return cardWidth;

        if (cardWidth + step * (count - 1) > maxWidth)
            step = MathF.Max(MathF.Floor((maxWidth - cardWidth) / (count - 1)), Sx(MinStep));

        return cardWidth + step * (count - 1);
    }

    private void DrawCardTexture(DrawingHandleScreen handle, Texture texture, float x, float y, int scale, CardKey key, TimeSpan now, float widthFactor)
    {
        var cardWidth = CardWidth * scale * _k;
        var cardHeight = CardHeight * scale * _k;
        var offset = Vector2.Zero;
        var alpha = 1f;
        if (_appear.TryGetValue(key, out var start))
        {
            var t = (float) (now - start).TotalSeconds / AppearTime;
            if (t < 1f)
            {
                var ease = EaseOut(t);
                offset = AppearFrom * _k * (1f - ease);
                alpha = 0.15f + 0.85f * ease;
            }
        }

        var width = MathF.Max(1f, MathF.Round(cardWidth * widthFactor));
        var left = MathF.Round(x + offset.X) + MathF.Round((cardWidth - width) / 2f);
        var top = MathF.Round(y + offset.Y);
        handle.DrawTextureRect(texture, UIBox2.FromDimensions(left, top, width, cardHeight), Color.White.WithAlpha(alpha));
    }

    private float ShakeX(int seat, int hand, TimeSpan now)
    {
        if (!_shake.TryGetValue((seat, hand), out var start))
            return 0f;

        var t = (float) (now - start).TotalSeconds / ShakeTime;
        if (t < 0f || t >= 1f)
            return 0f;

        return MathF.Round(MathF.Sin(t * 45f) * (1f - t) * 5f * _k);
    }

    private void DrawHand(DrawingHandleScreen handle, int seat, int index, BlackjackHand hand, float x, float y, int scale, float step, TimeSpan now)
    {
        var cs = scale * _k;
        for (var i = 0; i < hand.Cards.Count; i++)
        {
            var card = hand.Cards[i];
            DrawCardTexture(handle, Face(card), x + step * i, y, scale, new CardKey(seat, index, i, card), now, 1f);
        }

        if (hand.State == BlackjackHandState.Bust && hand.Cards.Count > 0)
        {
            var width = CardWidth * cs + step * (hand.Cards.Count - 1);
            handle.DrawRect(UIBox2.FromDimensions(x, y, width, CardHeight * cs), StoriesBlackjackPalette.Lose.WithAlpha(0.2f));
        }
    }

    private void DrawGlow(DrawingHandleScreen handle, UIBox2 box, TimeSpan now)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin((float) now.TotalSeconds * 4f);
        for (var i = 4; i >= 1; i--)
        {
            var alpha = (0.1f + 0.16f * pulse) * (5 - i) / 4f;
            handle.DrawRect(Grow(box, _k * (i + 2)), StoriesBlackjackPalette.Gold.WithAlpha(alpha), false);
        }

        for (var d = 1; d <= 2 * _k; d++)
        {
            handle.DrawRect(Grow(box, d), StoriesBlackjackPalette.Gold, false);
        }
    }

    private void DrawRing(DrawingHandleScreen handle, float x, float centreY, TimeSpan now)
    {
        var state = _ui["turn-ring"];
        var frame = StoriesBlackjackArt.FrameAt(state, (float) now.TotalSeconds);
        var size = RingSize * _k;
        handle.DrawTextureRect(state.GetFrame(RsiDirection.South, frame), UIBox2.FromDimensions(MathF.Round(x), MathF.Round(centreY - size / 2f), size, size));
    }

    private static Color TotalColor(int total)
    {
        return total > BlackjackRules.Target
            ? StoriesBlackjackPalette.Lose
            : total == BlackjackRules.Target ? StoriesBlackjackPalette.Gold : StoriesBlackjackPalette.Text;
    }

    private void BuildTexts(BlackjackTableComponent table)
    {
        _seatTexts = new SeatText[table.Seats.Count];
        for (var s = 0; s < table.Seats.Count; s++)
        {
            var seat = table.Seats[s];
            var text = new SeatText { Name = seat.Name, Hands = new HandText[seat.Hands.Count] };
            var bets = 0;
            var totals = string.Empty;
            for (var h = 0; h < seat.Hands.Count; h++)
            {
                var hand = seat.Hands[h];
                var handText = new HandText();
                text.Hands[h] = handText;
                bets += hand.Bet;
                if (hand.Cards.Count == 0)
                    continue;

                var total = BlackjackRules.Total(hand.Cards, out var soft);
                handText.TotalColor = TotalColor(total);
                handText.Compact = Loc.GetString("stories-blackjack-panel-total", ("total", total));
                handText.Total = soft ? Loc.GetString("stories-blackjack-panel-total-soft", ("total", total)) : handText.Compact;
                totals += (totals.Length > 0 ? " | " : string.Empty) + handText.Compact;
                if (table.Wager && hand.Bet > 0)
                    handText.Bet = Loc.GetString("stories-blackjack-panel-bet", ("amount", hand.Bet));

                BuildPlaque(table, hand, handText);
            }

            text.Totals = totals;
            if (table.Wager && bets > 0)
                text.BetSum = Loc.GetString("stories-blackjack-panel-bet", ("amount", bets));

            if (table.Wager && seat.Bet > 0)
                text.Stake = Loc.GetString("stories-blackjack-panel-bet", ("amount", seat.Bet));

            _seatTexts[s] = text;
        }

        _dealerText = null;
        if (table.DealerHand.Count > 0)
        {
            var total = BlackjackRules.Total(table.DealerHand, out var soft);
            _dealerText = Loc.GetString(soft ? "stories-blackjack-panel-total-soft" : "stories-blackjack-panel-total", ("total", total));
            if (table.DealerHidden)
                _dealerText += " + " + Loc.GetString("stories-blackjack-panel-hidden");

            _dealerColor = total > BlackjackRules.Target
                ? StoriesBlackjackPalette.Lose
                : total == BlackjackRules.Target && !table.DealerHidden ? StoriesBlackjackPalette.Gold : StoriesBlackjackPalette.Text;
        }
    }

    private static void BuildPlaque(BlackjackTableComponent table, BlackjackHand hand, HandText text)
    {
        var net = Math.Max(0, hand.Payout - hand.Bet);
        text.Win = $"+{net}";
        if (hand.State == BlackjackHandState.Bust)
        {
            text.Plaque = Loc.GetString("stories-blackjack-result-bust");
            text.PlaqueColor = StoriesBlackjackPalette.Lose;
            return;
        }

        if (table.Phase != BlackjackPhase.Payout)
        {
            if (hand.State != BlackjackHandState.Blackjack)
                return;

            text.Plaque = Loc.GetString("stories-blackjack-result-blackjack-plain");
            text.PlaqueColor = StoriesBlackjackPalette.Gold;
            return;
        }

        switch (hand.Result)
        {
            case BlackjackResult.Win:
                text.Plaque = table.Wager
                    ? Loc.GetString("stories-blackjack-result-win", ("amount", net))
                    : Loc.GetString("stories-blackjack-result-win-plain");
                text.PlaqueColor = StoriesBlackjackPalette.Win;
                break;
            case BlackjackResult.Blackjack:
                text.Plaque = table.Wager
                    ? Loc.GetString("stories-blackjack-result-blackjack", ("amount", net))
                    : Loc.GetString("stories-blackjack-result-blackjack-plain");
                text.PlaqueColor = StoriesBlackjackPalette.Gold;
                break;
            case BlackjackResult.Push:
                text.Plaque = Loc.GetString("stories-blackjack-result-push");
                break;
            case BlackjackResult.Lose:
                text.Plaque = Loc.GetString("stories-blackjack-result-lose");
                text.PlaqueColor = StoriesBlackjackPalette.Lose;
                break;
        }
    }

    private void DrawPlaque(DrawingHandleScreen handle, string text, Color color, float cx, float cy)
    {
        var width = TextWidth(_bold, text) + Sx(18);
        var height = _bold.GetLineHeight(_u) + Sx(8);
        var rect = UIBox2.FromDimensions(MathF.Round(cx - width / 2f), MathF.Round(cy - height / 2f), width, height);
        handle.DrawRect(rect, StoriesBlackjackPalette.Glass);
        handle.DrawRect(rect, color, false);
        handle.DrawRect(Grow(rect, -1f), color.WithAlpha(0.55f), false);
        DrawCentred(handle, _bold, text, rect.Left + width / 2f, rect.Top + Sx(4), color);
    }

    private void DrawEffects(DrawingHandleScreen handle, int seat, int index, string win, Vector2 origin, TimeSpan now)
    {
        if (!_wins.TryGetValue((seat, index), out var start) || _table == null)
            return;

        var t = (float) (now - start).TotalSeconds;
        if (t < 0f || t > EffectTime)
            return;

        if (t < SparkTime)
        {
            var life = t / SparkTime;
            for (var i = 0; i < SparkCount; i++)
            {
                var angle = -MathF.PI / 2f + (i / (float) SparkCount - 0.5f) * MathF.PI * 1.6f + ((i * 7) % 5 - 2) * 0.08f;
                var speed = (110f + i * 53 % 70) * _k;
                var position = origin + new Vector2(MathF.Cos(angle) * speed * t, MathF.Sin(angle) * speed * t + 180f * _k * t * t);
                var size = (i % 3 == 0 ? 3 : 2) * _k;
                var color = i % 2 == 0 ? StoriesBlackjackPalette.Gold : StoriesBlackjackPalette.GoldBright;
                handle.DrawRect(UIBox2.FromDimensions(MathF.Round(position.X), MathF.Round(position.Y), size, size), color.WithAlpha(1f - life));
            }
        }

        if (!_table.Wager)
            return;

        var rise = EaseOut(t / 1.2f) * Sx(60);
        var alpha = t < 1f ? 1f : 1f - (t - 1f) / (EffectTime - 1f);
        DrawCentred(handle, _bold, win, origin.X, origin.Y - Sx(28) - rise, StoriesBlackjackPalette.GoldBright.WithAlpha(Math.Clamp(alpha, 0f, 1f)));
    }

    private void Paint(DrawingHandleScreen handle, BlackjackTableComponent table)
    {
        _u = MathF.Max(UIScale, 1f);
        _k = Math.Max(1, (int) MathF.Floor(_u + 0.001f));
        if (_seatTexts.Length != table.Seats.Count)
            return;

        var now = _timing.RealTime;
        var width = PixelSize.X;
        var centre = MathF.Round(width / 2f);

        DrawDealer(handle, table, now, centre, width);

        if (_mySeat >= 0 && _mySeat < table.Seats.Count && table.Seats[_mySeat].Occupied)
        {
            DrawOwn(handle, table, now, centre, width);
            _others.Clear();
            for (var i = 0; i < table.Seats.Count; i++)
            {
                if (i != _mySeat && table.Seats[i].Occupied)
                    _others.Add(i);
            }

            var left = (_others.Count + 1) / 2;
            var slotWidth = Sx(SideWidth) - Sx(6);
            for (var j = 0; j < _others.Count; j++)
            {
                var onLeft = j < left;
                var row = Math.Min(onLeft ? j : j - left, 1);
                var x = onLeft ? Sx(3) : width - Sx(SideWidth) + Sx(3);
                var y = Sx(row == 0 ? SlotTopFirst : SlotTopSecond);
                DrawSlot(handle, table, _others[j], x, y, slotWidth, now);
            }

            return;
        }

        var count = Math.Max(1, table.Seats.Count);
        var each = width / count;
        for (var s = 0; s < table.Seats.Count; s++)
        {
            if (table.Seats[s].Occupied)
                DrawSlot(handle, table, s, MathF.Round(s * each) + Sx(3), Sx(SlotTopRow), each - Sx(6), now);
        }
    }

    private void DrawDealer(DrawingHandleScreen handle, BlackjackTableComponent table, TimeSpan now, float centre, float width)
    {
        DrawCentred(handle, _bold, _dealerLabel, centre, Sx(DealerLabelY), StoriesBlackjackPalette.Gold);

        var known = table.DealerHand.Count;
        var count = known + (table.DealerHidden ? 1 : 0);
        if (count == 0)
            return;

        var cs = 2 * _k;
        var maxWidth = width - 2f * Sx(SideWidth) - Sx(90);
        var total = Fit(count, BigStep, 2, maxWidth, out var step);
        var x = MathF.Round(centre - total / 2f);
        var y = Sx(DealerCardsY);
        var flip = (float) (now - _flipStart).TotalSeconds;
        for (var i = 0; i < count; i++)
        {
            var hole = i >= known;
            var card = hole ? default : table.DealerHand[i];
            var texture = hole ? Back() : Face(card);
            var factor = 1f;
            if (!hole && i == _flipIndex && flip >= 0f && flip < FlipTime)
            {
                var f = flip / FlipTime;
                texture = f < 0.5f ? Back() : Face(card);
                factor = MathF.Max(f < 0.5f ? 1f - 2f * f : 2f * f - 1f, 0.02f);
            }

            DrawCardTexture(handle, texture, x + step * i, y, 2, new CardKey(-1, 0, i, card), now, factor);
        }

        if (_dealerText == null)
            return;

        DrawText(handle, _bold, _dealerText, x + total + Sx(12), y + CardHeight * cs / 2f - _bold.GetLineHeight(_u) / 2f, _dealerColor);
    }

    private void DrawOwn(DrawingHandleScreen handle, BlackjackTableComponent table, TimeSpan now, float centre, float width)
    {
        var seat = table.Seats[_mySeat];
        var seatText = _seatTexts[_mySeat];
        var myTurn = table.Phase == BlackjackPhase.Turns && table.ActiveSeat == _mySeat;
        var labelY = Sx(PlayerLabelY);
        DrawCentred(handle, _bold, _youLabel, centre, labelY, StoriesBlackjackPalette.Gold);
        if (myTurn)
            DrawRing(handle, centre - TextWidth(_bold, _youLabel) / 2f - RingSize * _k - Sx(4), labelY + _bold.GetLineHeight(_u) / 2f, now);

        var count = seat.Hands.Count;
        if (count == 0 || seatText.Hands.Length < count)
        {
            DrawStake(handle, seatText, seat.Bet, centre);
            return;
        }

        var gap = Sx(HandGap);
        var maxWidth = (width - 2f * Sx(SideWidth) - Sx(24) - gap * (count - 1)) / count;
        var sum = gap * (count - 1);
        for (var i = 0; i < count; i++)
        {
            sum += Fit(seat.Hands[i].Cards.Count, BigStep, 2, maxWidth, out _);
        }

        var x = MathF.Round(centre - sum / 2f);
        var y = Sx(PlayerCardsY);
        var cardHeight = CardHeight * 2f * _k;
        var lineHeight = _font.GetLineHeight(_u);
        for (var i = 0; i < count; i++)
        {
            var hand = seat.Hands[i];
            var handText = seatText.Hands[i];
            var handWidth = Fit(hand.Cards.Count, BigStep, 2, maxWidth, out var step);
            if (hand.Cards.Count == 0)
                continue;

            var hx = x + ShakeX(_mySeat, i, now);
            var box = UIBox2.FromDimensions(hx, y, handWidth, cardHeight);
            DrawHand(handle, _mySeat, i, hand, hx, y, 2, step, now);

            if (myTurn && count > 1 && table.ActiveHand != i)
                handle.DrawRect(box, StoriesBlackjackPalette.Shade.WithAlpha(0.5f));
            else if (myTurn && table.ActiveHand == i && hand.State == BlackjackHandState.Playing)
                DrawGlow(handle, box, now);

            var middle = hx + handWidth / 2f;
            DrawCentred(handle, _bold, handText.Total, middle, Sx(PlayerTextY), handText.TotalColor);
            if (handText.Bet != null)
                DrawCentred(handle, _font, handText.Bet, middle, Sx(PlayerTextY) + lineHeight + Sx(1), StoriesBlackjackPalette.Gold);

            if (handText.Plaque != null)
                DrawPlaque(handle, handText.Plaque, handText.PlaqueColor, middle, y + cardHeight / 2f);

            DrawEffects(handle, _mySeat, i, handText.Win, new Vector2(middle, y), now);
            x += handWidth + gap;
        }
    }

    private void DrawStake(DrawingHandleScreen handle, SeatText text, int bet, float centre)
    {
        if (text.Stake == null)
            return;

        var name = StoriesBlackjackArt.ChipState(bet);
        var size = 56f * _k;
        var y = Sx(PlayerCardsY + 24);
        handle.DrawTextureRect(_ui[name].GetFrame(RsiDirection.South, 0), UIBox2.FromDimensions(MathF.Round(centre - size / 2f), y, size, size));
        DrawCentred(handle, _bold, text.Stake, centre, y + size + Sx(6), StoriesBlackjackPalette.Gold);
    }

    private void DrawSlot(DrawingHandleScreen handle, BlackjackTableComponent table, int index, float x, float y, float width, TimeSpan now)
    {
        var seat = table.Seats[index];
        var text = _seatTexts[index];
        var active = table.Phase == BlackjackPhase.Turns && table.ActiveSeat == index;
        var ring = RingSize * _k;
        var nameX = x;
        var nameWidth = width;
        if (active)
        {
            DrawRing(handle, x, y + _font.GetLineHeight(_u) / 2f, now);
            nameX += ring + Sx(2);
            nameWidth -= ring + Sx(2);
        }

        var nameFont = active ? _bold : _font;
        if (text.FitWidth != nameWidth || text.FitActive != active)
        {
            text.Fitted = FitText(nameFont, text.Name, nameWidth);
            text.FitWidth = nameWidth;
            text.FitActive = active;
        }

        DrawText(handle, nameFont, text.Fitted, nameX, y, active ? StoriesBlackjackPalette.Gold : StoriesBlackjackPalette.Text);

        var cardsY = y + Sx(SlotCardsOffset);
        var cardHeight = CardHeight * _k;
        var count = seat.Hands.Count;
        var lineY = cardsY + cardHeight + Sx(3);
        var lineHeight = _small.GetLineHeight(_u);
        var centre = x + width / 2f;

        if (active)
            DrawGlow(handle, UIBox2.FromDimensions(x + Sx(2), cardsY - Sx(2), width - Sx(4), cardHeight + Sx(4)), now);

        if (count == 0 || text.Hands.Length < count)
        {
            if (text.Stake != null)
                DrawCentred(handle, _small, text.Stake, centre, cardsY + cardHeight / 2f - lineHeight / 2f, StoriesBlackjackPalette.Gold);

            return;
        }

        var gap = Sx(6);
        var maxWidth = (width - gap * (count - 1)) / count;
        var sum = gap * (count - 1);
        for (var i = 0; i < count; i++)
        {
            sum += Fit(seat.Hands[i].Cards.Count, SmallStep, 1, maxWidth, out _);
        }

        var hx = MathF.Max(x, MathF.Round(centre - sum / 2f));
        for (var i = 0; i < count; i++)
        {
            var hand = seat.Hands[i];
            var handWidth = Fit(hand.Cards.Count, SmallStep, 1, maxWidth, out var step);
            if (hand.Cards.Count > 0)
            {
                var px = hx + ShakeX(index, i, now);
                DrawHand(handle, index, i, hand, px, cardsY, 1, step, now);
                DrawEffects(handle, index, i, text.Hands[i].Win, new Vector2(px + handWidth / 2f, cardsY), now);
            }

            hx += handWidth + gap;
        }

        if (count == 1 && seat.Hands[0].Cards.Count > 0)
            DrawCentred(handle, _small, text.Hands[0].Total, centre, lineY, text.Hands[0].TotalColor);
        else if (text.Totals.Length > 0)
            DrawCentred(handle, _small, text.Totals, centre, lineY, StoriesBlackjackPalette.Text);

        lineY += lineHeight;
        if (text.BetSum != null)
        {
            DrawCentred(handle, _small, text.BetSum, centre, lineY, StoriesBlackjackPalette.Gold);
            lineY += lineHeight;
        }

        for (var i = 0; i < count; i++)
        {
            var handText = text.Hands[i];
            if (handText.Plaque == null)
                continue;

            if (handText.PlaqueFitWidth != width)
            {
                handText.PlaqueFitted = FitText(_small, handText.Plaque, width);
                handText.PlaqueFitWidth = width;
            }

            DrawCentred(handle, _small, handText.PlaqueFitted, centre, lineY, handText.PlaqueColor);
            lineY += lineHeight;
        }
    }
}
