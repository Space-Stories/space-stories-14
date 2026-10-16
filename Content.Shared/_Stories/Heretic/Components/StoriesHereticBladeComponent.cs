using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticBladeComponent : Component
{
    [DataField]
    public float TeleportMin = 10f;

    [DataField]
    public float TeleportMax = 20f;

    [DataField]
    public TimeSpan BreakTime = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier? BreakSound = new SoundPathSpecifier("/Audio/Effects/glass_break1.ogg");
}
