using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Stories.Heretic.UI;

/// <summary>
/// Раскладывает узлы знаний по колонкам (пути) и строкам (ступени), рисует подложки колонок и линии связей.
/// Узлы создаются из данных, поэтому их нельзя описать в XAML.
/// </summary>
public sealed class StoriesHereticKnowledgeTreeControl : LayoutContainer
{
    private const float ColumnWidth = 170f;
    private const float NodeWidth = 150f;
    private const float RowHeight = 150f;
    private const float HeaderHeight = 40f;
    private const float Padding = 10f;
    private const float LineThickness = 3f;

    private readonly Dictionary<string, StoriesHereticKnowledgeNodeControl> _controls = new();
    private readonly Vector2[] _quad = new Vector2[4];
    private IReadOnlyList<StoriesHereticKnowledgeColumn> _columns = Array.Empty<StoriesHereticKnowledgeColumn>();

    public event Action<string>? OnLearn;

    public void SetNodes(IReadOnlyList<StoriesHereticKnowledgeColumn> columns, IReadOnlyList<StoriesHereticKnowledgeNode> nodes)
    {
        RemoveAllChildren();
        _controls.Clear();
        _columns = columns;

        for (var i = 0; i < columns.Count; i++)
        {
            var header = new Label
            {
                Text = columns[i].Name,
                Align = Label.AlignMode.Center,
                SetWidth = ColumnWidth,
                FontColorOverride = StoriesHereticPalette.Get(columns[i].Path).Accent,
                StyleClasses = { "LabelHeading" },
            };
            AddChild(header);
            SetPosition(header, new Vector2(i * ColumnWidth, Padding));
        }

        var maxRow = 0;
        foreach (var node in nodes)
        {
            var control = new StoriesHereticKnowledgeNodeControl(node);
            var id = node.Id;
            control.OnPressed += _ => OnLearn?.Invoke(id);

            AddChild(control);
            // Не-стартовые знания начинаются со ступени 1 — она и есть первая строка.
            var row = Math.Max(node.Stage - 1, 0);
            SetPosition(control, new Vector2(node.Column * ColumnWidth + (ColumnWidth - NodeWidth) / 2, HeaderHeight + Padding + row * RowHeight));
            _controls[id] = control;
            maxRow = Math.Max(maxRow, row);
        }

        MinSize = new Vector2(columns.Count * ColumnWidth, HeaderHeight + Padding * 2 + (maxRow + 1) * RowHeight);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var columnWidth = ColumnWidth * UIScale;

        // Подложка колонки — тёмный тон пути, под заголовком — черта цвета пути.
        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            var left = i * columnWidth + 4 * UIScale;
            var right = (i + 1) * columnWidth - 4 * UIScale;
            if (column.BridgeTo != null)
            {
                var mid = (left + right) / 2;
                handle.DrawRect(new UIBox2(left, 0, mid, PixelSize.Y), StoriesHereticPalette.Get(column.Path).Dark.WithAlpha(0.25f));
                handle.DrawRect(new UIBox2(mid, 0, right, PixelSize.Y), StoriesHereticPalette.Get(column.BridgeTo).Dark.WithAlpha(0.25f));
                continue;
            }

            var palette = StoriesHereticPalette.Get(column.Path);
            handle.DrawRect(new UIBox2(left, 0, right, PixelSize.Y), palette.Dark.WithAlpha(0.35f));
            handle.DrawRect(new UIBox2(left, (HeaderHeight - 4) * UIScale, right, (HeaderHeight - 2) * UIScale), palette.Accent.WithAlpha(0.6f));
        }

        // Связи рисуются под узлами (дети рисуются после родителя) и упираются в центр печати.
        // Вниз линия идёт от нижнего края узла, чтобы не перечёркивать название и цену; в пределах строки — между печатями.
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

    /// <summary>
    /// <see cref="DrawingHandleBase.DrawLine"/> рисует только 1 px, поэтому толстая линия — полоса из двух треугольников.
    /// Вершины пишутся в заранее созданный массив: выражение коллекции в <c>Span</c> компилируется
    /// в InlineArray, который не проходит проверку песочницы клиента (клиент не загрузится).
    /// </summary>
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
