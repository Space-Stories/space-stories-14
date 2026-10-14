using System.Linq;

namespace Content.Client._Stories.Heretic.UI;

/// <summary>
/// Порядок путей в окне — кольцо дерева Paradise. Побочные знания-мосты стоят между соседями.
/// Это раскладка окна, а не правило игры: доступ к знаниям задают их требования.
/// </summary>
public static class StoriesHereticRing
{
    public static readonly string[] Order =
    [
        "STHereticPathAsh", "STHereticPathMoon", "STHereticPathLock", "STHereticPathFlesh",
        "STHereticPathVoid", "STHereticPathBlade", "STHereticPathRust", "STHereticPathCosmos",
    ];

    /// <summary>
    /// Колонки слева направо: общая, затем пути по кольцу (начиная с Пепла) и мосты между ними.
    /// Мост к пути, которого нет в окне, ставится рядом со своим путём с той стороны, где по кольцу лежит сосед.
    /// Мост записывается как (путь слева, путь справа по кольцу).
    /// </summary>
    public static List<(string? Path, string? BridgeTo)> BuildColumns(
        IReadOnlyCollection<string> paths,
        IReadOnlyCollection<(string A, string B)> bridges)
    {
        var shown = Order.Where(paths.Contains).ToList();
        // Пути, которых нет в кольце (тестовые), — в конец по алфавиту.
        shown.AddRange(paths.Where(p => !Order.Contains(p)).OrderBy(p => p));

        var normalized = new HashSet<(string Left, string Right)>();
        foreach (var (a, b) in bridges)
        {
            normalized.Add(IsClockwiseNeighbour(a, b) ? (a, b) : (b, a));
        }

        var columns = new List<(string? Path, string? BridgeTo)> { (null, null) };
        foreach (var path in shown)
        {
            // Полумост слева: сосед слева по кольцу не показан.
            foreach (var (left, right) in normalized)
            {
                if (right == path && !shown.Contains(left))
                    columns.Add((path, left));
            }

            columns.Add((path, null));

            foreach (var (left, right) in normalized)
            {
                if (left == path)
                    columns.Add((path, right));
            }
        }

        return columns;
    }

    public static int ColumnOf(List<(string? Path, string? BridgeTo)> columns, string? path, IReadOnlyList<string> bridge)
    {
        if (bridge.Count == 2)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var (p, to) = columns[i];
                if (to != null && ((p == bridge[0] && to == bridge[1]) || (p == bridge[1] && to == bridge[0])))
                    return i;
            }
        }

        for (var i = 0; i < columns.Count; i++)
        {
            if (path != null && columns[i].Path == path && columns[i].BridgeTo == null)
                return i;
        }

        return 0;
    }

    /// <summary>
    /// <paramref name="b"/> — следующий после <paramref name="a"/> путь по кольцу.
    /// </summary>
    private static bool IsClockwiseNeighbour(string a, string b)
    {
        var ia = Array.IndexOf(Order, a);
        var ib = Array.IndexOf(Order, b);
        return ia >= 0 && ib >= 0 && (ia + 1) % Order.Length == ib;
    }
}
