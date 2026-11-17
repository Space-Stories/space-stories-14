using Content.Shared._Stories.Blackjack;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Stories.Blackjack.UI;

[UsedImplicitly]
public sealed partial class StoriesBlackjackGameBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private const int BottomMargin = -170;
    private const float LockTime = 1.5f;
    private const float LowTimer = 0.25f;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    [ViewVariables]
    private StoriesBlackjackPanel? _panel;

    private readonly record struct Snapshot(BlackjackPhase Phase, int ActiveSeat, int ActiveHand, int Cards, BlackjackHandState State, int Bet, int Chips);

    private int _mySeat = -1;
    private int _betSum;
    private bool _collapsed;
    private bool _locked;
    private TimeSpan _lockUntil;
    private Snapshot _lockSnapshot;
    private BlackjackPhase _lastPhase = BlackjackPhase.Idle;
    private int _countdown = int.MinValue;

    protected override void Open()
    {
        base.Open();

        _panel = new StoriesBlackjackPanel();
        _panel.OnChip += value => SetSum(_betSum + value);
        _panel.OnClear += () => SetSum(0);
        _panel.OnAllIn += () => SetSum(BetCap());
        _panel.OnRepeat += () => SetSum(MySeat()?.LastBet ?? 0);
        _panel.OnPlace += PlaceBet;
        _panel.OnAction += SendAction;
        _panel.OnCollapse += OnCollapsePressed;
        _panel.OnLeave += OnLeavePressed;
        _panel.OnFrame += OnPanelFrame;

        _ui.StateRoot.AddChild(_panel);
        LayoutContainer.SetAnchorAndMarginPreset(_panel, LayoutContainer.LayoutPreset.CenterBottom);
        LayoutContainer.SetMarginLeft(_panel, 0);
        LayoutContainer.SetMarginRight(_panel, 0);
        LayoutContainer.SetMarginTop(_panel, BottomMargin);
        LayoutContainer.SetMarginBottom(_panel, BottomMargin);
        LayoutContainer.SetGrowVertical(_panel, LayoutContainer.GrowDirection.Begin);
        LayoutContainer.SetGrowHorizontal(_panel, LayoutContainer.GrowDirection.Both);
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_panel == null || Table() is not { } table)
            return;

        var me = _player.LocalEntity is { } local ? EntMan.GetNetEntity(local) : NetEntity.Invalid;
        _mySeat = FindSeat(table, me);

        if (table.Phase != _lastPhase)
        {
            if (table.Phase == BlackjackPhase.Betting)
                _betSum = 0;

            _lastPhase = table.Phase;
        }

        if (MySeat() is { Bet: > 0 })
            _betSum = 0;

        if (_locked && Take() != _lockSnapshot)
            _locked = false;

        _panel.SetCanvas(table, _mySeat);
        Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_panel is { Disposed: false, Parent: not null })
            _panel.Orphan();

        _panel = null;
    }
}
