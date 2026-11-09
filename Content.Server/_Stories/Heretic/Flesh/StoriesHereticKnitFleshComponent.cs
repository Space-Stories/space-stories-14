using Content.Shared.EntityEffects.Effects.Damage;
using Robust.Shared.Audio;

namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticKnitFleshComponent : Component
{
    [DataField]
    public TimeSpan CorpseTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan LivingTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan HealTime = TimeSpan.FromSeconds(1);

    [DataField]
    public DistributedHealthChange Heal = new() { Damage = new() { ["Brute"] = -10, ["Burn"] = -5 } };

    [DataField]
    public LocId HealSpeech = "stories-heretic-knit-speech";

    [DataField]
    public float DistanceThreshold = 1.5f;

    [DataField]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/_Stories/Heretic/staff_healing.ogg");

    [DataField]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/_Stories/Heretic/slice.ogg");
}
