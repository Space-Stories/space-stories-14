namespace Content.Server._Stories.Heretic;

[RegisterComponent, Access(typeof(StoriesHereticObjectiveSystem))]
public sealed partial class StoriesHereticObjectiveConditionComponent : Component
{
    [DataField(required: true)]
    public StoriesHereticObjectiveKind Kind;

    [DataField(required: true)]
    public LocId Title;
}

public enum StoriesHereticObjectiveKind : byte
{
    Knowledge,
    Sacrifice,
    HeadSacrifice,
}
