using Content.Shared._Stories.Heretic.Components;
using Content.Shared.DoAfter;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Heretic.Events;

[ByRefEvent]
public readonly record struct StoriesHereticKnowledgeLearnedEvent(EntProtoId Knowledge);

[ByRefEvent]
public record struct StoriesHereticRitualEffectEvent(
    Entity<StoriesHereticComponent> Heretic,
    EntityUid Rune,
    EntProtoId Ritual,
    StoriesHereticRitualEffect Effect,
    HashSet<EntityUid> Nearby,
    List<EntityUid> Consumed)
{
    public bool Handled;
    public bool Success;
    public string? FailReason;
}

[ByRefEvent]
public readonly record struct StoriesHereticGraspHitEvent(EntityUid Heretic, EntityUid Target);

[ByRefEvent]
public readonly record struct StoriesHereticMarkTriggeredEvent(EntityUid Heretic, EntityUid Target, ProtoId<TagPrototype> Path, int Hops);

[ByRefEvent]
public readonly record struct StoriesHereticBladeHitEvent(EntityUid Heretic, EntityUid Target, EntityUid Blade);

[Serializable, NetSerializable]
public sealed partial class StoriesHereticDrawRuneDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    [DataField]
    public string Rune = string.Empty;

    private StoriesHereticDrawRuneDoAfterEvent()
    {
    }

    public StoriesHereticDrawRuneDoAfterEvent(NetCoordinates coordinates, string rune)
    {
        Coordinates = coordinates;
        Rune = rune;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class StoriesHereticHarvestDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StoriesHereticBladeBreakDoAfterEvent : SimpleDoAfterEvent;
