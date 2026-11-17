using System.Numerics;
using Content.Shared._Stories.Blackjack;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Stories.Blackjack.UI;

public sealed partial class StoriesBlackjackCanvas : Control
{
    public const int DesignWidth = 664;
    public const int DesignHeight = 312;

    private const float SideWidth = 140f;
    private const float DealerLabelY = 0f;
    private const float DealerCardsY = 22f;
    private const float PlayerLabelY = 136f;
    private const float PlayerCardsY = 158f;
    private const float PlayerTextY = 272f;
    private const float SlotTopFirst = 4f;
    private const float SlotTopSecond = 150f;
    private const float SlotTopRow = 140f;
    private const float SlotCardsOffset = 20f;
    private const float CardWidth = 40f;
    private const float CardHeight = 56f;
    private const float BigStep = 34f;
    private const float SmallStep = 14f;
    private const float MinStep = 8f;
    private const float HandGap = 18f;
    private const float AppearTime = 0.25f;
    private const float FlipTime = 0.3f;
    private const float ShakeTime = 0.3f;
    private const float EffectTime = 1.6f;
    private const float SparkTime = 0.9f;
    private const int SparkCount = 10;
    private const int NameLimit = 24;

    private static readonly Vector2 AppearFrom = new(90f, -70f);

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ILogManager _logMan = default!;
    [Dependency] private IResourceCache _cache = default!;

    private readonly RSI _ui;
    private readonly Font _font;
    private readonly Font _bold;
    private readonly Font _small;
    private readonly ISawmill _sawmill;
    private readonly Dictionary<BlackjackCard, Texture> _faces = new();
    private readonly Dictionary<CardKey, TimeSpan> _appear = new();
    private readonly HashSet<CardKey> _keys = new();
    private readonly Dictionary<(int, int), BlackjackHandState> _states = new();
    private readonly Dictionary<(int, int), TimeSpan> _shake = new();
    private readonly Dictionary<(int, int), TimeSpan> _wins = new();
    private readonly HashSet<(int, int)> _handKeys = new();
    private readonly List<(int, int)> _stale = new();
    private readonly List<CardKey> _dead = new();

    private RSI? _cards;
    private ResPath _cardsPath;
    private Texture? _back;
    private BlackjackTableComponent? _table;
    private int _mySeat = -1;
    private bool _initialised;
    private bool _prevHidden;
    private int _prevDealerCount;
    private int _flipIndex = -1;
    private TimeSpan _flipStart;
    private bool _failed;
    private float _u = 1f;
    private int _k = 1;

    private readonly record struct CardKey(int Seat, int Hand, int Index, BlackjackCard Card);

    private sealed class HandText
    {
        public string Total = string.Empty;
        public string Compact = string.Empty;
        public Color TotalColor = StoriesBlackjackPalette.Text;
        public string? Bet;
        public string? Plaque;
        public Color PlaqueColor = StoriesBlackjackPalette.Text;
        public string Win = string.Empty;
        public string PlaqueFitted = string.Empty;
        public float PlaqueFitWidth = -1f;
    }

    private sealed class SeatText
    {
        public string Name = string.Empty;
        public string Fitted = string.Empty;
        public float FitWidth = -1f;
        public bool FitActive;
        public string Totals = string.Empty;
        public string? BetSum;
        public string? Stake;
        public HandText[] Hands = Array.Empty<HandText>();
    }

    private SeatText[] _seatTexts = Array.Empty<SeatText>();
    private string? _dealerText;
    private Color _dealerColor = StoriesBlackjackPalette.Text;
    private string _dealerLabel = string.Empty;
    private string _youLabel = string.Empty;

    public StoriesBlackjackCanvas()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _logMan.GetSawmill("blackjack.canvas");
        _ui = _cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / StoriesBlackjackArt.UiSprite).RSI;
        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 12);
        _bold = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 12);
        _small = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        MinSize = new Vector2(DesignWidth, DesignHeight);
        MouseFilter = MouseFilterMode.Ignore;
        _dealerLabel = Loc.GetString("stories-blackjack-panel-dealer");
        _youLabel = Loc.GetString("stories-blackjack-panel-you");
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var scale = MathF.Max(UIScale, 1f) / UIScale;
        return new Vector2(DesignWidth, DesignHeight) * scale;
    }

    public void SetState(BlackjackTableComponent table, int mySeat)
    {
        if (_cards == null || _cardsPath != table.CardSprite)
        {
            _cardsPath = table.CardSprite;
            _cards = _cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / table.CardSprite).RSI;
            _faces.Clear();
            _back = null;
        }

        _failed = false;
        var now = _timing.RealTime;
        var old = now - TimeSpan.FromSeconds(1000);

        if (_initialised && _prevHidden && !table.DealerHidden && _prevDealerCount < table.DealerHand.Count)
        {
            _flipIndex = _prevDealerCount;
            _flipStart = now;
            _appear[new CardKey(-1, 0, _flipIndex, table.DealerHand[_flipIndex])] = old;
        }

        _keys.Clear();
        _handKeys.Clear();
        for (var i = 0; i < table.DealerHand.Count; i++)
        {
            _keys.Add(new CardKey(-1, 0, i, table.DealerHand[i]));
        }

        if (table.DealerHidden)
            _keys.Add(new CardKey(-1, 0, table.DealerHand.Count, default));

        for (var s = 0; s < table.Seats.Count; s++)
        {
            var seat = table.Seats[s];
            for (var h = 0; h < seat.Hands.Count; h++)
            {
                var hand = seat.Hands[h];
                _handKeys.Add((s, h));
                for (var i = 0; i < hand.Cards.Count; i++)
                {
                    _keys.Add(new CardKey(s, h, i, hand.Cards[i]));
                }

                var key = (s, h);
                var known = _states.TryGetValue(key, out var previous);
                if (hand.State == BlackjackHandState.Bust && (!known || previous != BlackjackHandState.Bust))
                    _shake[key] = _initialised ? now : old;

                _states[key] = hand.State;
                if (table.Phase == BlackjackPhase.Payout
                    && (hand.Result == BlackjackResult.Win || hand.Result == BlackjackResult.Blackjack)
                    && !_wins.ContainsKey(key))
                {
                    _wins[key] = _initialised ? now : old;
                }
            }
        }

        foreach (var key in _keys)
        {
            if (!_appear.ContainsKey(key))
                _appear[key] = _initialised ? now : old;
        }

        _dead.Clear();
        foreach (var key in _appear.Keys)
        {
            if (!_keys.Contains(key))
                _dead.Add(key);
        }

        foreach (var key in _dead)
        {
            _appear.Remove(key);
        }

        PruneHands(_states);
        PruneHands(_shake);
        if (table.Phase != BlackjackPhase.Payout)
            _wins.Clear();
        else
            PruneHands(_wins);

        BuildTexts(table);
        _prevHidden = table.DealerHidden;
        _prevDealerCount = table.DealerHand.Count;
        _initialised = true;
        _table = table;
        _mySeat = mySeat;
    }

    private void PruneHands<T>(Dictionary<(int, int), T> dictionary)
    {
        _stale.Clear();
        foreach (var key in dictionary.Keys)
        {
            if (!_handKeys.Contains(key))
                _stale.Add(key);
        }

        foreach (var key in _stale)
        {
            dictionary.Remove(key);
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_table == null || _cards == null || _failed)
            return;

        try
        {
            Paint(handle, _table);
        }
        catch (Exception e)
        {
            _failed = true;
            _sawmill.Error($"Blackjack canvas failed: {e}");
        }
    }
}
