using Content.Shared.AlertLevel;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.StationsControl;

[RegisterComponent]
public sealed partial class StationsControlConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<AlertLevelPrototype>> AllowedAlertLevels = new();

    [DataField, AutoNetworkedField]
    public bool AllowAnyAlertLevel = false;
}