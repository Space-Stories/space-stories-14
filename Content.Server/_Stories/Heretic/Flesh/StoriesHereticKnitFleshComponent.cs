using Content.Shared.EntityEffects.Effects.Damage;
using Robust.Shared.Audio;

namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// На еретике: параметры заклинания «Сшить плоть» и извлечения органов без операции.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticKnitFleshComponent : Component
{
    [DataField]
    public TimeSpan CorpseTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan LivingTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan HealTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Лечение слуги по группам урона: 10 Brute и 5 Burn, распределённые по имеющемуся урону.
    /// </summary>
    [DataField]
    public DistributedHealthChange Heal = new() { Damage = new() { ["Brute"] = -10, ["Burn"] = -5 } };

    /// <summary>
    /// Шёпот при касании слуги.
    /// </summary>
    [DataField]
    public LocId HealSpeech = "stories-heretic-knit-speech";

    /// <summary>
    /// Дальше этого извлечение органа и лечение прерываются.
    /// </summary>
    [DataField]
    public float DistanceThreshold = 1.5f;

    [DataField]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/_Stories/Heretic/staff_healing.ogg");

    [DataField]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/_Stories/Heretic/slice.ogg");
}
