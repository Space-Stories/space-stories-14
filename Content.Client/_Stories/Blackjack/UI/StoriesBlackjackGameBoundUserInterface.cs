using Content.Shared._Stories.Blackjack;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Stories.Blackjack.UI;

[UsedImplicitly]
public sealed partial class StoriesBlackjackGameBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private const int BottomMargin = -170;

    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    [ViewVariables]
    private StoriesBlackjackPanel? _panel;

    protected override void Open()
    {
        base.Open();

        _panel = new StoriesBlackjackPanel();
        _panel.OnBet += amount => SendMessage(new BlackjackBetMessage(amount));
        _panel.OnAction += (action, hand) => SendMessage(new BlackjackActionMessage(action, hand));
        _panel.OnLeave += () => SendMessage(new BlackjackLeaveMessage());
        _panel.OnClose += Close;

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

        if (_panel == null || !EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table))
            return;

        var me = _player.LocalEntity is { } local ? EntMan.GetNetEntity(local) : NetEntity.Invalid;
        _panel.SetState(table, me);
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
