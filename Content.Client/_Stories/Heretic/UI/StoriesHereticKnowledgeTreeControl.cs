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
    private const float BridgeGap = 14f;
    private const int GradientSteps = 24;
    private const float BridgeAlpha = 0.3f;

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
        var slots = new Dictionary<(int Column, int Row), int>();
        var slotOf = new Dictionary<string, int>();
        var perColumn = new int[columns.Count];
        foreach (var node in nodes)
        {
            if (columns[node.Column].BridgeTo == null)
                continue;

            var key = (node.Column, Math.Max(node.Stage - 1, 0));
            slots[key] = slots.GetValueOrDefault(key) + 1;
            slotOf[node.Id] = slots[key] - 1;
            perColumn[node.Column] = Math.Max(perColumn[node.Column], slots[key]);
        }

        var x = 0f;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = i;
            _columnX[i] = x;
            _columnWidth[i] = columns[i].BridgeTo != null
                ? Math.Max(ColumnWidth, perColumn[i] * NodeWidth + (perColumn[i] + 1) * BridgeGap)
                : ColumnWidth + (nodes.Any(n => n.Side && n.Column == column) ? SideWidth : 0);
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
            float left;
            if (slotOf.TryGetValue(id, out var slot))
            {
                var count = slots[(node.Column, row)];
                var total = count * NodeWidth + (count - 1) * BridgeGap;
                left = _columnX[node.Column] + (_columnWidth[node.Column] - total) / 2 + slot * (NodeWidth + BridgeGap);
            }
            else
            {
                left = _columnX[node.Column] + (node.Side
                    ? ColumnWidth + (SideWidth - NodeWidth) / 2
                    : (ColumnWidth - NodeWidth) / 2);
            }
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
            var column = _columns[i];
            var left = (_columnX[i] + 4) * UIScale;
            var right = (_columnX[i] + _columnWidth[i] - 4) * UIScale;
            if (column.BridgeTo != null)
            {
                var bridgeIsLeft = i + 1 < _columns.Count && _columns[i + 1].Path == column.Path && _columns[i + 1].BridgeTo == null;
                var from = StoriesHereticPalette.Get(bridgeIsLeft ? column.BridgeTo : column.Path).Dark;
                var to = StoriesHereticPalette.Get(bridgeIsLeft ? column.Path : column.BridgeTo).Dark;
                left = _columnX[i] * UIScale;
                right = (_columnX[i] + _columnWidth[i]) * UIScale;
                var step = (right - left) / GradientSteps;
                for (var s = 0; s < GradientSteps; s++)
                {
                    var color = Color.InterpolateBetween(from, to, (s + 0.5f) / GradientSteps).WithAlpha(BridgeAlpha);
                    handle.DrawRect(new UIBox2(left + s * step, 0, left + (s + 1) * step, PixelSize.Y), color);
                }

                continue;
            }

            var palette = StoriesHereticPalette.Get(column.Path);
            handle.DrawRect(new UIBox2(left, 0, right, PixelSize.Y), palette.Dark.WithAlpha(0.35f));
            handle.DrawRect(new UIBox2(left, (HeaderHeight - 4) * UIScale, right, (HeaderHeight - 2) * UIScale), palette.Accent.WithAlpha(0.6f));
        }

        var thickness = LineThickness * UIScale;
        var rails = new HashSet<(int Column, int Stage)>();
        foreach (var to in _controls.Values)
        {
            var color = LineColor(to.Node);
            var bridge = _columns[to.Node.Column].BridgeTo != null;
            if (bridge)
                rails.Add((to.Node.Column, to.Node.Stage));

            foreach (var required in to.Node.Requires)
            {
                if (!_controls.TryGetValue(required, out var from))
                    continue;

                if (bridge && from.Node.Stage == to.Node.Stage && _columns[from.Node.Column].BridgeTo == null)
                    continue;

                var start = to.Node.Stage > from.Node.Stage
                    ? (Vector2) from.PixelPosition + new Vector2(from.SealCenter.X, from.PixelSize.Y)
                    : (Vector2) from.PixelPosition + from.SealCenter;

                DrawThickLine(handle, start, (Vector2) to.PixelPosition + to.SealCenter, thickness, color);
            }
        }

        foreach (var (column, stage) in rails)
        {
            var middle = (_columnX[column] + _columnWidth[column] / 2) * UIScale;
            StoriesHereticKnowledgeNodeControl? leftNode = null;
            StoriesHereticKnowledgeNodeControl? rightNode = null;
            StoriesHereticKnowledgeNodeControl? best = null;
            foreach (var node in _controls.Values)
            {
                if (node.Node.Column != column || node.Node.Stage != stage)
                    continue;

                if (best == null || node.Node.State < best.Node.State)
                    best = node;

                foreach (var required in node.Node.Requires)
                {
                    if (!_controls.TryGetValue(required, out var from) ||
                        from.Node.Stage != stage ||
                        _columns[from.Node.Column].BridgeTo != null)
                        continue;

                    var x = from.PixelPosition.X + from.SealCenter.X;
                    if (x < middle)
                    {
                        if (leftNode == null || x > leftNode.PixelPosition.X + leftNode.SealCenter.X)
                            leftNode = from;
                    }
                    else if (rightNode == null || x < rightNode.PixelPosition.X + rightNode.SealCenter.X)
                    {
                        rightNode = from;
                    }
                }
            }

            if (best == null || leftNode == null && rightNode == null)
                continue;

            var y = best.PixelPosition.Y + best.SealCenter.Y;
            var railStart = leftNode != null
                ? new Vector2(leftNode.PixelPosition.X + leftNode.SealCenter.X, y)
                : new Vector2(_columnX[column] * UIScale, y);
            var railEnd = rightNode != null
                ? new Vector2(rightNode.PixelPosition.X + rightNode.SealCenter.X, y)
                : new Vector2((_columnX[column] + _columnWidth[column]) * UIScale, y);
            DrawThickLine(handle, railStart, railEnd, thickness, LineColor(best.Node));
        }
    }

    private static Color LineColor(StoriesHereticKnowledgeNode node)
    {
        var palette = StoriesHereticPalette.Get(node.Path);
        return node.State switch
        {
            StoriesHereticNodeState.Learned => palette.Accent,
            StoriesHereticNodeState.Available => palette.Accent.WithAlpha(0.55f),
            _ => palette.Accent.WithAlpha(0.2f),
        };
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
