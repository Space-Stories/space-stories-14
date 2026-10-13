using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Stories.Heretic.UI;

/// <summary>
/// Раскладывает узлы знаний по колонкам (пути) и строкам (ступени) и рисует линии связей.
/// Узлы создаются из данных, поэтому их нельзя описать в XAML.
/// </summary>
public sealed class StoriesHereticKnowledgeTreeControl : LayoutContainer
{
    private const float ColumnWidth = 210f;
    private const float RowHeight = 70f;
    private const float NodeWidth = 190f;
    private const float NodeHeight = 44f;
    private const float HeaderHeight = 30f;

    private static readonly Color LearnedColor = Color.FromHex("#58a65c");
    private static readonly Color LineColor = Color.FromHex("#666666");

    private readonly Dictionary<string, Control> _buttons = new();
    private IReadOnlyList<StoriesHereticKnowledgeNode> _nodes = Array.Empty<StoriesHereticKnowledgeNode>();

    public event Action<string>? OnLearn;

    public void SetNodes(IReadOnlyList<string> columns, IReadOnlyList<StoriesHereticKnowledgeNode> nodes)
    {
        RemoveAllChildren();
        _buttons.Clear();
        _nodes = nodes;

        for (var i = 0; i < columns.Count; i++)
        {
            var header = new Label
            {
                Text = columns[i],
                Align = Label.AlignMode.Center,
                SetWidth = NodeWidth,
            };
            AddChild(header);
            SetPosition(header, new Vector2(i * ColumnWidth, 0));
        }

        var maxStage = 0;
        foreach (var node in nodes)
        {
            var button = new Button
            {
                Text = Loc.GetString("stories-heretic-ui-node", ("name", node.Name), ("cost", node.Cost)),
                ToolTip = node.Reason == null ? node.Description : $"{node.Description}\n\n{node.Reason}",
                Disabled = node.State != StoriesHereticNodeState.Available,
                SetSize = new Vector2(NodeWidth, NodeHeight),
                ClipText = true,
            };

            if (node.State == StoriesHereticNodeState.Learned)
                button.ModulateSelfOverride = LearnedColor;

            var id = node.Id;
            button.OnPressed += _ => OnLearn?.Invoke(id);

            AddChild(button);
            SetPosition(button, new Vector2(node.Column * ColumnWidth, HeaderHeight + node.Stage * RowHeight));
            _buttons[id] = button;
            maxStage = Math.Max(maxStage, node.Stage);
        }

        MinSize = new Vector2(columns.Count * ColumnWidth, HeaderHeight + (maxStage + 1) * RowHeight);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var node in _nodes)
        {
            if (!_buttons.TryGetValue(node.Id, out var to))
                continue;

            foreach (var required in node.Requires)
            {
                if (!_buttons.TryGetValue(required, out var from))
                    continue;

                var start = (from.Position + new Vector2(NodeWidth / 2, NodeHeight)) * UIScale;
                var end = (to.Position + new Vector2(NodeWidth / 2, 0)) * UIScale;
                handle.DrawLine(start, end, node.State == StoriesHereticNodeState.Learned ? LearnedColor : LineColor);
            }
        }
    }
}
