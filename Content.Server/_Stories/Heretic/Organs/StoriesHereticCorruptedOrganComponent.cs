namespace Content.Server._Stories.Heretic.Organs;

[RegisterComponent, Access(typeof(StoriesHereticCorruptedOrganSystem))]
public sealed partial class StoriesHereticCorruptedOrganComponent : Component
{
    [DataField(required: true)]
    public StoriesHereticCorruptedOrganKind Kind;

    [DataField]
    public TimeSpan NextEffect;

    [DataField]
    public TimeSpan SatedUntil;

    [DataField]
    public float BurstChance = 0.25f;
}

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
