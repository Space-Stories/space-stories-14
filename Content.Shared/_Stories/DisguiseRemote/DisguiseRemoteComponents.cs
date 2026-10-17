using Content.Shared.Actions;
using Content.Shared.Cloning;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.DisguiseRemote;

/// <summary>
/// Пульт переобличия: при клике по гуманоиду копирует его облик, вещи и имя,
/// выдаёт владельцу способность переключаться между обликами и исчезает.
/// </summary>
[RegisterComponent]
public sealed partial class DisguiseRemoteComponent : Component;

/// <summary>
/// Висит на том, кто применил пульт. Оба облика хранятся снимками на паузной карте,
/// как личности генокрада: на снимке неактивного облика лежит его комплект вещей.
/// </summary>
[RegisterComponent]
public sealed partial class DisguisedComponent : Component
{
    /// <summary>
    /// Что копируется при смене облика. Должны совпадать для обоих снимков, иначе облики разъедутся по компонентам.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> Settings = "ChangelingCloningSettings";

    /// <summary>
    /// То же самое, но без копирования вещей: свои настоящие вещи переезжают на снимок сами.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> SelfSettings = "STDisguiseSelfCloningSettings";

    [DataField]
    public EntProtoId Action = "STActionDisguiseToggle";

    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Снимок настоящего облика.
    /// </summary>
    [DataField]
    public EntityUid? OriginalForm;

    /// <summary>
    /// Снимок скопированного облика.
    /// </summary>
    [DataField]
    public EntityUid? DisguiseForm;

    [DataField]
    public bool Disguised;

    /// <summary>
    /// Был ли у жертвы виден значок щита разума в момент копирования.
    /// </summary>
    [DataField]
    public bool MindShieldVisible;
}

public sealed partial class DisguiseToggleActionEvent : InstantActionEvent;
