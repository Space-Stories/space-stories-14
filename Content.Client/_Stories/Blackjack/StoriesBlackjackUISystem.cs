using Content.Shared._Stories.Blackjack;

namespace Content.Client._Stories.Blackjack;

public sealed partial class StoriesBlackjackUISystem : EntitySystem
{
    private static readonly BlackjackUiKey[] Keys = [BlackjackUiKey.Lobby, BlackjackUiKey.Game];

    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StoriesBlackjackVisualsSystem _visuals = default!;

    public override void Initialize()
    {
        base.Initialize();

        _visuals.TableStateChanged += OnTableStateChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _visuals.TableStateChanged -= OnTableStateChanged;
    }

    private void OnTableStateChanged(EntityUid uid)
    {
        try
        {
            foreach (var key in Keys)
            {
                if (_ui.TryGetOpenUi(uid, key, out var bui))
                    bui.Update();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update blackjack UI for {ToPrettyString(uid)}: {e}");
        }
    }
}
