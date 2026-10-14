namespace Content.Server._Stories.Heretic.Organs;

/// <summary>
/// Орган, вставленный Мансусом в принесённую жертву (tg corrupted_organ).
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticCorruptedOrganSystem))]
public sealed partial class StoriesHereticCorruptedOrganComponent : Component
{
    [DataField(required: true)]
    public StoriesHereticCorruptedOrganKind Kind;

    [DataField]
    public TimeSpan NextEffect;

    /// <summary>
    /// Шанс взорваться при извлечении из живого тела.
    /// </summary>
    [DataField]
    public float BurstChance = 0.25f;
}

/// <summary>
/// На теле, в котором есть испорченные органы, — для подписки на события тела (речь, питьё).
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticCorruptedOrganSystem))]
public sealed partial class StoriesHereticCorruptedBodyComponent : Component;

public enum StoriesHereticCorruptedOrganKind : byte
{
    Eyes,
    Tongue,
    Liver,
    Stomach,
    Heart,
    Lungs,
    Appendix,
}
