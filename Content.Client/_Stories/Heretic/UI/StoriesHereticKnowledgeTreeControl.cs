using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Stories.Heretic.UI;

public sealed class StoriesHereticKnowledgeTreeControl : LayoutContainer
{
    private const float ColumnWidth = 170f;
    private const float SideWidth = 160f;
    private const float NodeWidth = 150f;
    private const float RowHeight = 150f;
    private const float HeaderHeight = 40f;
    private const float Padding = 10f;
    private const float LineThickness = 3f;

    private readonly Dictionary<string, StoriesHereticKnowledgeNodeControl> _controls = new();
    private readonly Vector2[] _quad = new Vector2[4];
    private IReadOnlyList<StoriesHereticKnowledgeColumn> _columns = Array.Empty<StoriesHereticKnowledgeColumn>();
    private float[] _columnX = Array.Empty<float>();
    private float[] _columnWidth = Array.Empty<float>();

    public event Action<string>? OnLearn;

    public void SetNodes(IReadOnlyList<StoriesHereticKnowledgeColumn> columns, IReadOnlyList<StoriesHereticKnowledgeNode> nodes)
    {
        RemoveAllChildren();
        _controls.Clear();
        _columns = columns;

        _columnX = new float[columns.Count];
        _columnWidth = new float[columns.Count];
        var x = 0f;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = i;
            _columnX[i] = x;
            _columnWidth[i] = ColumnWidth + (nodes.Any(n => n.Side && n.Column == column) ? SideWidth : 0);
            x += _columnWidth[i];
        }

        for (var i = 0; i < columns.Count; i++)
        {
            var header = new Label
            {
                Text = columns[i].Name,
                Align = Label.AlignMode.Center,
                SetWidth = _columnWidth[i],
                FontColorOverride = StoriesHereticPalette.Get(columns[i].Path).Accent,
                StyleClasses = { "LabelHeading" },
            };
            AddChild(header);
            SetPosition(header, new Vector2(_columnX[i], Padding));
        }

        var maxRow = 0;
        foreach (var node in nodes)
        {
            var control = new StoriesHereticKnowledgeNodeControl(node);
            var id = node.Id;
            control.OnPressed += _ => OnLearn?.Invoke(id);

            AddChild(control);
            var row = Math.Max(node.Stage - 1, 0);
            var left = _columnX[node.Column] + (node.Side
                ? ColumnWidth + (SideWidth - NodeWidth) / 2
                : (ColumnWidth - NodeWidth) / 2);
            SetPosition(control, new Vector2(left, HeaderHeight + Padding + row * RowHeight));
            _controls[id] = control;
            maxRow = Math.Max(maxRow, row);
        }

        MinSize = new Vector2(x, HeaderHeight + Padding * 2 + (maxRow + 1) * RowHeight);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        for (var i = 0; i < _columns.Count; i++)
        {
            var palette = StoriesHereticPalette.Get(_columns[i].Path);
            var left = (_columnX[i] + 4) * UIScale;
            var right = (_columnX[i] + _columnWidth[i] - 4) * UIScale;
            handle.DrawRect(new UIBox2(left, 0, right, PixelSize.Y), palette.Dark.WithAlpha(0.35f));
            handle.DrawRect(new UIBox2(left, (HeaderHeight - 4) * UIScale, right, (HeaderHeight - 2) * UIScale), palette.Accent.WithAlpha(0.6f));
        }

        foreach (var to in _controls.Values)
        {
            var palette = StoriesHereticPalette.Get(to.Node.Path);
            var color = to.Node.State switch
            {
                StoriesHereticNodeState.Learned => palette.Accent,
                StoriesHereticNodeState.Available => palette.Accent.WithAlpha(0.55f),
                _ => palette.Accent.WithAlpha(0.2f),
            };

            foreach (var required in to.Node.Requires)
            {
                if (!_controls.TryGetValue(required, out var from))
                    continue;

                var start = to.Node.Stage > from.Node.Stage
                    ? (Vector2) from.PixelPosition + new Vector2(from.SealCenter.X, from.PixelSize.Y)
                    : (Vector2) from.PixelPosition + from.SealCenter;

                DrawThickLine(handle, start, (Vector2) to.PixelPosition + to.SealCenter, LineThickness * UIScale, color);
            }
        }
    }

    private void DrawThickLine(DrawingHandleScreen handle, Vector2 from, Vector2 to, float thickness, Color color)
    {
        var direction = to - from;
        if (direction.LengthSquared() < 0.01f)
            return;

        var normal = new Vector2(-direction.Y, direction.X) / direction.Length() * (thickness / 2);
        _quad[0] = from + normal;
        _quad[1] = from - normal;
        _quad[2] = to + normal;
        _quad[3] = to - normal;
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, _quad, color);
    }
}
