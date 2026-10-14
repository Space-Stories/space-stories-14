using Content.Shared._Stories.Heretic.Components;
using Content.Shared.DoAfter;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Heretic.Events;

/// <summary>
/// Поднимается на еретике после выдачи знания.
/// </summary>
[ByRefEvent]
public readonly record struct StoriesHereticKnowledgeLearnedEvent(EntProtoId Knowledge);

/// <summary>
/// Широковещательное событие для ритуалов с <see cref="StoriesHereticRitualEffect"/> (кроме Knowledge).
/// Обработчик проверяет условия, при успехе добавляет поглощаемые сущности в <see cref="Consumed"/>
/// и ставит <see cref="Success"/>; при неудаче — <see cref="FailReason"/>, но только если на руне нашлось,
/// с чем работать (иначе подсказку даст другой ритуал).
/// </summary>
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

/// <summary>
/// Поднимается на еретике после попадания Хваткой по существу.
/// </summary>
[ByRefEvent]
public readonly record struct StoriesHereticGraspHitEvent(EntityUid Heretic, EntityUid Target);

/// <summary>
/// Широковещательное: удар клинком по цели с меткой своего пути.
/// </summary>
[ByRefEvent]
public readonly record struct StoriesHereticMarkTriggeredEvent(EntityUid Heretic, EntityUid Target, ProtoId<TagPrototype> Path, int Hops);

/// <summary>
/// Поднимается на еретике при попадании клинком еретика.
/// </summary>
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

/// <summary>Расчленение трупа клинком; отделяемый орган передаётся как <c>used</c> DoAfter.</summary>
[Serializable, NetSerializable]
public sealed partial class StoriesHereticDismemberDoAfterEvent : SimpleDoAfterEvent;

/// <summary>Извлечение органа заклинанием «Сшить плоть»; орган передаётся как <c>used</c> DoAfter.</summary>
[Serializable, NetSerializable]
public sealed partial class StoriesHereticKnitExtractDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StoriesHereticMansusLinkDoAfterEvent : SimpleDoAfterEvent;
