using Content.Shared.Actions;
using Content.Shared.Cloning;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.DisguiseRemote;

[RegisterComponent]
public sealed partial class DisguiseRemoteComponent : Component;

[RegisterComponent]
public sealed partial class DisguisedComponent : Component
{
    [DataField]
    public ProtoId<CloningSettingsPrototype> Settings = "ChangelingCloningSettings";

    [DataField]
    public ProtoId<CloningSettingsPrototype> SelfSettings = "STDisguiseSelfCloningSettings";

    [DataField]
    public EntProtoId Action = "STActionDisguiseToggle";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public EntityUid? OriginalForm;

    [DataField]
    public EntityUid? DisguiseForm;

    [DataField]
    public bool Disguised;

    [DataField]
    public bool MindShieldVisible;
}

public sealed partial class DisguiseToggleActionEvent : InstantActionEvent;
