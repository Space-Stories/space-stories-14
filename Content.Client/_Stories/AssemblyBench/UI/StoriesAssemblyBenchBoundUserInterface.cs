using System.Linq;
using Content.Shared._Stories.AssemblyBench;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Stories.AssemblyBench.UI;

public readonly record struct StoriesAssemblyBenchStepView(string Id, string Name, string Requirement, EntProtoId Icon, bool IsTool);

public readonly record struct StoriesAssemblyBenchView(
    ResPath Sprite,
    IReadOnlyList<StoriesAssemblyBenchStepView> Steps,
    int Current,
    int Completed,
    TimeSpan WorkEnd,
    float WorkDuration,
    EntProtoId Result,
    string ResultName);

[UsedImplicitly]
public sealed partial class StoriesAssemblyBenchBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _proto = default!;

    [ViewVariables]
    private StoriesAssemblyBenchWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StoriesAssemblyBenchWindow>();
        _window.OnApply += step => SendPredictedMessage(new AssemblyBenchApplyMessage(step));
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent(Owner, out AssemblyBenchComponent? bench))
            return;

        var system = EntMan.System<StoriesAssemblyBenchSystem>();
        var steps = bench.Steps
            .Select(s => new StoriesAssemblyBenchStepView(s.Id, Loc.GetString(s.Name), system.GetRequirement(s), s.Icon, s.Tool != null))
            .ToList();

        _window.UpdateState(new StoriesAssemblyBenchView(bench.Sprite,
            steps,
            bench.Step,
            bench.Completed,
            bench.WorkEnd,
            bench.WorkDuration,
            bench.Result,
            _proto.Index(bench.Result).Name));
    }
}
