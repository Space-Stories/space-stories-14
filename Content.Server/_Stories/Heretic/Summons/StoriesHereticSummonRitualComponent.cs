using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Summons;

/// <summary>
/// На прототипе ритуала с эффектом Summon: кого призвать.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticSummonSystem))]
public sealed partial class StoriesHereticSummonRitualComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Mob;
}
