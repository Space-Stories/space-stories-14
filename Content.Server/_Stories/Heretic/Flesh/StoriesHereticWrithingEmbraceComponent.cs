using Content.Shared.Damage;
using Content.Shared.EntityEffects.Effects.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// Извивающиеся объятия: на еретике лечат его слуг поблизости, на остальных рвут тело.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticWrithingEmbraceComponent : Component
{
    /// <summary>
    /// Раз в столько аура лечит слуг и раз в столько не-еретик получает рану.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [DataField]
    public float Range = 15f;

    /// <summary>
    /// Лечение каждого слуги за импульс (aura_healing в tg: по 3 в секунду).
    /// </summary>
    [DataField]
    public DistributedHealthChange Heal = new() { Damage = new() { ["Brute"] = -3, ["Burn"] = -3, ["Airloss"] = -3 } };

    /// <summary>
    /// Рана не-еретику за импульс (критическая резаная рана tg на каждой части тела).
    /// </summary>
    [DataField]
    public DamageSpecifier Wound = new() { DamageDict = new() { ["Slash"] = 10 } };

    [DataField]
    public float WoundBleed = 3f;

    /// <summary>
    /// Сколько ран успеет нанести мантия чужаку: по одной на часть тела.
    /// </summary>
    [DataField]
    public int Wounds = 6;

    [DataField]
    public int WoundsLeft;

    [DataField]
    public EntityUid? Wearer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
