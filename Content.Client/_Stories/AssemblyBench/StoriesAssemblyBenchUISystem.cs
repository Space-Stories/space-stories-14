using Content.Shared._Stories.AssemblyBench;

namespace Content.Client._Stories.AssemblyBench;

public sealed partial class StoriesAssemblyBenchUISystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AssemblyBenchComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<AssemblyBenchComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (_ui.TryGetOpenUi(ent.Owner, AssemblyBenchUiKey.Key, out var bui))
                bui.Update();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update assembly bench window for {ToPrettyString(ent)}: {e}");
        }
    }
}
