using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticSacrificedComponent : Component
{
    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(3);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new() { { "Blunt", 15 } },
    };
}
