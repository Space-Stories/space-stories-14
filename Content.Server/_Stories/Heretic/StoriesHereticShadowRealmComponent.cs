using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticSacrificeSystem))]
public sealed partial class StoriesHereticShadowRealmComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ReturnTime;

    [DataField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDamage;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new() { { "Cold", 4 } },
    };

    [DataField]
    public EntityCoordinates ReturnCoordinates;

    [DataField]
    public TimeSpan HallucinationTime = TimeSpan.FromSeconds(6);
}
