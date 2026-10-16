using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// На еретике: Хватка ставит метку пути.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticGraspMarkComponent : Component
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Path;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);
}
