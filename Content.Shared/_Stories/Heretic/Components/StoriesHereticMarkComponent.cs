using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Метка пути на цели. Срабатывает от удара клинком того же пути.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class StoriesHereticMarkComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype> Path;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Сколько раз метка уже перескакивала (для меток, которые распространяются).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Hops;
}
