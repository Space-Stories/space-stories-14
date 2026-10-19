using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.PocketDimension;

[RegisterComponent]
public sealed partial class PocketDimensionStickerComponent : Component
{
    [DataField]
    public TimeSpan StickDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan UnstickDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier StickSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg", AudioParams.Default.WithVolume(-6f).WithMaxDistance(3f));

    [ViewVariables]
    public EntityUid? Map;

    [ViewVariables]
    public EntityUid? InnerDoor;

    [ViewVariables]
    public EntityUid? OuterDoor;

    [ViewVariables]
    public EntityCoordinates? Exit;
}

[RegisterComponent]
public sealed partial class PocketDimensionDoorComponent : Component
{
    [DataField]
    public TimeSpan PassDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public SoundSpecifier PassSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg", AudioParams.Default.WithVolume(-10f).WithMaxDistance(3f));

    [ViewVariables]
    public EntityUid? Sticker;
}

[RegisterComponent]
public sealed partial class PocketDimensionWallComponent : Component
{
    [DataField]
    public int HitsRequired = 10;

    [DataField]
    public TimeSpan HitCooldown = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier HitSound = new SoundCollectionSpecifier("GenericHit");
}

[RegisterComponent]
public sealed partial class PocketDimensionMapComponent : Component
{
    [ViewVariables]
    public EntityUid? Sticker;

    [ViewVariables]
    public int Hits;

    [ViewVariables]
    public TimeSpan NextHit;
}

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
