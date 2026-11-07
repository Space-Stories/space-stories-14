using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Heretic.Bridges;

[Serializable, NetSerializable]
public enum StoriesHereticVoidPrisonVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum StoriesHereticVoidPrisonState : byte
{
    Opening,
    Active,
    Closing,
}
