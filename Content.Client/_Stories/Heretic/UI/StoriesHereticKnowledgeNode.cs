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
/// </summary>
public sealed record StoriesHereticKnowledgeColumn(string? Path, string Name);

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
    string? Reason);
