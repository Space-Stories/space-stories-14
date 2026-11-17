using Content.Shared._Stories.Blackjack;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._Stories.Blackjack.UI;

[UsedImplicitly]
public sealed partial class StoriesBlackjackLobbyBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPlayerManager _player = default!;

    [ViewVariables]
    private StoriesBlackjackLobbyWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StoriesBlackjackLobbyWindow>();
        _window.OnSit += seat => SendMessage(new BlackjackSitMessage(seat));
        _window.OnReady += ready => SendMessage(new BlackjackReadyMessage(ready));
        _window.OnLeave += () => SendMessage(new BlackjackLeaveMessage());
        _window.OnSpectate += () => SendMessage(new BlackjackSpectateMessage());
        _window.OnCashier += (op, amount) => SendMessage(new BlackjackCashierMessage(op, amount));
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table))
            return;

        var me = _player.LocalEntity is { } local ? EntMan.GetNetEntity(local) : NetEntity.Invalid;
        _window.UpdateState(table, me);
    }
}
