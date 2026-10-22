using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Inventory;

namespace Content.Shared._Stories.Vision.Events;

[ByRefEvent]
public record struct RefreshVisionEvent : IInventoryRelayEvent
{
    public Color? AmbientColor = null;
    public bool DrawFov = true;
    public bool DrawLighting = true;
    public bool DrawShadows = true;
    public bool IsActive = false;
    public int Priority = -1;
    public string? Shader = null;
    public Color? ThermalAmbientColor = null;
    public string? ThermalShader = null;
    public bool ThermalVision = false;

    public RefreshVisionEvent()
    {
    }

    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.OUTERCLOTHING;
}

public sealed partial class ToggleVisionActionEvent : InstantActionEvent;

[DataDefinition]
public sealed partial class ToggleVisionAlertEvent : BaseAlertEvent;
