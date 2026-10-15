using System.Linq;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Stories.Heretic.UI;

/// <summary>
/// Собирает узлы дерева из прототипов знаний и <see cref="StoriesHereticComponent"/> владельца.
/// </summary>
[UsedImplicitly]
public sealed partial class StoriesHereticKnowledgeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _proto = default!;

    [ViewVariables]
    private StoriesHereticKnowledgeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StoriesHereticKnowledgeWindow>();
        _window.OnLearn += id => SendMessage(new StoriesHereticLearnMessage(id));
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent(Owner, out StoriesHereticComponent? heretic))
            return;

        var system = EntMan.System<StoriesHereticSystem>();
        var ent = new Entity<StoriesHereticComponent>(Owner, heretic);

        // Колонки идут по кольцу Paradise, мосты — между соседними путями.
        var paths = system.AllKnowledge
            .Where(k => k.Comp.Path != null)
            .Select(k => k.Comp.Path!.Value.Id)
            .Distinct()
            .ToList();

        var bridges = system.AllKnowledge
            .Where(k => k.Comp.Bridge.Count == 2)
            .Select(k => (k.Comp.Bridge[0].Id, k.Comp.Bridge[1].Id))
            .Distinct()
            .ToList();

        var layout = StoriesHereticRing.BuildColumns(paths, bridges);
        var columns = layout
            .Select(c => new StoriesHereticKnowledgeColumn(c.Path,
                c.Path == null
                    ? Loc.GetString("stories-heretic-ui-column-general")
                    : c.BridgeTo == null ? Loc.GetString($"stories-heretic-path-{c.Path}") : string.Empty,
                c.BridgeTo))
            .ToList();

        var nodes = new List<StoriesHereticKnowledgeNode>();
        foreach (var (id, knowledge) in system.AllKnowledge)
        {
            if (knowledge.Starting || !_proto.TryIndex(id, out var proto))
                continue;

            string? reason = null;
            var state = system.KnowsKnowledge(ent, id)
                ? StoriesHereticNodeState.Learned
                : system.CanLearn(ent, id, out reason)
                    ? StoriesHereticNodeState.Available
                    : StoriesHereticNodeState.Locked;

            var column = StoriesHereticRing.ColumnOf(layout, knowledge.Path?.Id, knowledge.Bridge.Select(b => b.Id).ToList());

            nodes.Add(new StoriesHereticKnowledgeNode(id,
                proto.Name,
                proto.Description,
                knowledge.Cost,
                column,
                knowledge.Stage,
                knowledge.Path?.Id,
                knowledge.Icon,
                knowledge.Requires.Select(r => r.Id).ToList(),
                state,
                state == StoriesHereticNodeState.Learned ? null : reason,
                knowledge.Side));
        }

        var pathName = heretic.Path is { } chosen
            ? Loc.GetString($"stories-heretic-path-{chosen.Id}")
            : Loc.GetString("stories-heretic-path-none");

        var ritualItems = heretic.KnowledgeRitualUsed
            ? null
            : string.Join(", ", system.GetKnowledgeRitualTags(ent).Select(t => Loc.GetString($"stories-heretic-tag-{t.Id}")));

        _window.UpdateState(heretic.Points, heretic.Path?.Id, pathName, ritualItems, columns, nodes);
    }
}
