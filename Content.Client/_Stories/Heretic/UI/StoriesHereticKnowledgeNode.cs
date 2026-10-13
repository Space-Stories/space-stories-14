namespace Content.Client._Stories.Heretic.UI;

public enum StoriesHereticNodeState : byte
{
    Learned,
    Available,
    Locked,
}

public sealed record StoriesHereticKnowledgeNode(
    string Id,
    string Name,
    string Description,
    int Cost,
    int Column,
    int Stage,
    IReadOnlyList<string> Requires,
    StoriesHereticNodeState State,
    string? Reason);
