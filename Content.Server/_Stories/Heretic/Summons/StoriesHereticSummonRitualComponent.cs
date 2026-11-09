using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Summons;

[RegisterComponent, Access(typeof(StoriesHereticSummonSystem))]
public sealed partial class StoriesHereticSummonRitualComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Mob;
}
