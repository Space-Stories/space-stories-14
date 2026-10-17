using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.PocketDimension;

/// <summary>
/// Наклейка «Изнанка»: приклеивается на стену и превращает её в дверь в личную комнату вне станции.
/// </summary>
[RegisterComponent]
public sealed partial class PocketDimensionStickerComponent : Component
{
    [DataField]
    public TimeSpan StickDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan UnstickDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Тихий звук при приклеивании и отклеивании.
    /// </summary>
    [DataField]
    public SoundSpecifier StickSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg", AudioParams.Default.WithVolume(-6f).WithMaxDistance(3f));

    /// <summary>
    /// Карта с комнатой. Создаётся при первом приклеивании и живёт, пока жива наклейка.
    /// </summary>
    [ViewVariables]
    public EntityUid? Map;

    [ViewVariables]
    public EntityUid? InnerDoor;

    /// <summary>
    /// Дверь на станции. Пока она есть, наклейка лежит в ней.
    /// </summary>
    [ViewVariables]
    public EntityUid? OuterDoor;

    /// <summary>
    /// Куда выходить из комнаты: место, откуда приклеивали наклейку.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates? Exit;
}

/// <summary>
/// Дверь Изнанки, внешняя или внутренняя.
/// </summary>
[RegisterComponent]
public sealed partial class PocketDimensionDoorComponent : Component
{
    [DataField]
    public TimeSpan PassDelay = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Тихий звук прохода, играет и у входа, и у выхода.
    /// </summary>
    [DataField]
    public SoundSpecifier PassSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg", AudioParams.Default.WithVolume(-10f).WithMaxDistance(3f));

    [ViewVariables]
    public EntityUid? Sticker;
}

/// <summary>
/// Стена комнаты, которую можно долбить, чтобы выбраться на станцию.
/// </summary>
[RegisterComponent]
public sealed partial class PocketDimensionWallComponent : Component
{
    [DataField]
    public int HitsRequired = 10;

    /// <summary>
    /// Пауза между засчитанными ударами, чтобы их нельзя было накликать за секунду.
    /// </summary>
    [DataField]
    public TimeSpan HitCooldown = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier HitSound = new SoundCollectionSpecifier("GenericHit");
}

/// <summary>
/// Метка карты Изнанки: на ней глушится связь.
/// </summary>
[RegisterComponent]
public sealed partial class PocketDimensionMapComponent : Component
{
    [ViewVariables]
    public EntityUid? Sticker;

    /// <summary>
    /// Сколько ударов по стенам уже нанесли. Общий на всех, кто сидит в комнате.
    /// </summary>
    [ViewVariables]
    public int Hits;

    [ViewVariables]
    public TimeSpan NextHit;
}

/// <summary>
/// Дверь показывает открытый разрыв, пока через неё кто-то проходит.
/// </summary>
[Serializable, NetSerializable]
public enum PocketDimensionDoorVisuals : byte
{
    Open,
}

[Serializable, NetSerializable]
public sealed partial class PocketDimensionStickDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class PocketDimensionUnstickDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class PocketDimensionPassDoAfterEvent : SimpleDoAfterEvent;
