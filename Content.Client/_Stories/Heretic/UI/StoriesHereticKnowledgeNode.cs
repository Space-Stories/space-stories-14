using Robust.Shared.Utility;

namespace Content.Client._Stories.Heretic.UI;

public enum StoriesHereticNodeState : byte
{
    Learned,
    Available,
    Locked,
}

/// <summary>
/// Колонка дерева. <see cref="Path"/> — ID тега пути, <c>null</c> — общие знания.
/// <see cref="BridgeTo"/> — второй путь моста; у колонки пути — <c>null</c>.
/// </summary>
public sealed record StoriesHereticKnowledgeColumn(string? Path, string Name, string? BridgeTo = null);

public sealed record StoriesHereticKnowledgeNode(
    string Id,
    string Name,
    string Description,
    int Cost,
    int Column,
    int Stage,
    string? Path,
    SpriteSpecifier? Icon,
    IReadOnlyList<string> Requires,
    StoriesHereticNodeState State,
    string? Reason,
    bool Side = false);
