using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Heretic;

[Serializable, NetSerializable]
public enum StoriesHereticKnowledgeUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class StoriesHereticLearnMessage(string knowledge) : BoundUserInterfaceMessage
{
    public readonly string Knowledge = knowledge;
}
