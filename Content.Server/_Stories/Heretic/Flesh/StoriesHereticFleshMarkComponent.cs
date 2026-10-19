using Content.Shared.Damage;

namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticFleshMarkComponent : Component
{
    [DataField]
    public float Bleed = 8f;

    [DataField]
    public DamageSpecifier Damage = new() { DamageDict = new() { { "Slash", 10 } } };
}
