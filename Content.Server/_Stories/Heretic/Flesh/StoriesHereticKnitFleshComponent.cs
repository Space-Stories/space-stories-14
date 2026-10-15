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
    public float HealBrute = 10;

    [DataField]
    public float HealBurn = 5;

    /// <summary>
    /// Дальше этого извлечение органа прерывается.
    /// </summary>
    [DataField]
    public float DistanceThreshold = 1.5f;

    [DataField]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/_Stories/Heretic/staff_healing.ogg");

    [DataField]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/_Stories/Heretic/slice.ogg");
}
