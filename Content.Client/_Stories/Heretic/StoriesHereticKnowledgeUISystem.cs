using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;

namespace Content.Client._Stories.Heretic;

public sealed partial class StoriesHereticKnowledgeUISystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<StoriesHereticComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (_ui.TryGetOpenUi(ent.Owner, StoriesHereticKnowledgeUiKey.Key, out var bui))
                bui.Update();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update heretic knowledge window for {ToPrettyString(ent)}: {e}");
        }
    }
}
