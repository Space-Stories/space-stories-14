namespace Content.Shared._Stories.Strip;

[ByRefEvent]
public record struct StoriesStripItemHiddenEvent(EntityUid? Viewer, bool Hidden = false);
