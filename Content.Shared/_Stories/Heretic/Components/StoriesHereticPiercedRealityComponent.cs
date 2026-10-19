using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticPiercedRealityComponent : Component
{
    [DataField]
    public float ExamineStaminaDamage = 20f;

    [DataField]
    public TimeSpan ExamineHallucinationTime = TimeSpan.FromSeconds(5);

    [DataField]
    public float TouchChance = 0.25f;

    [DataField]
    public DamageSpecifier TouchDamage = new()
    {
        DamageDict = new() { { "Slash", 20 } },
    };
}
