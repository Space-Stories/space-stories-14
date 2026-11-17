using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._Stories.Blackjack.UI;

public static class StoriesBlackjackArt
{
    public static readonly ResPath UiSprite = new("_Stories/Interface/Blackjack/ui.rsi");
    public static readonly int[] ChipValues = [5, 25, 100, 500];

    public static Texture? Icon(IResourceCache cache, string state)
    {
        var rsi = cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / UiSprite).RSI;
        return rsi.TryGetState(state, out var found) ? found.GetFrame(RsiDirection.South, 0) : null;
    }

    public static int FrameAt(RSI.State state, float time)
    {
        if (state.DelayCount <= 1 || state.AnimationLength <= 0f)
            return 0;

        var position = time % state.AnimationLength;
        var accumulated = 0f;
        for (var i = 0; i < state.DelayCount; i++)
        {
            accumulated += state.GetDelay(i);
            if (position < accumulated)
                return i;
        }

        return state.DelayCount - 1;
    }

    public static string ChipState(int bet)
    {
        var value = ChipValues[0];
        foreach (var chip in ChipValues)
        {
            if (bet >= chip)
                value = chip;
        }

        return $"chip-{value}";
    }
}
