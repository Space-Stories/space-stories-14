using Robust.Shared.Utility;

namespace Content.Client._Stories.Heretic.UI;

public enum StoriesHereticNodeState : byte
{
    Learned,
    Available,
    Locked,
}

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
    string? Reason,
    bool Side = false);
