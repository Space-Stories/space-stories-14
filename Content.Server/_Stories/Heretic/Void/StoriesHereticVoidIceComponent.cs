namespace Content.Server._Stories.Heretic.Void;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticVoidSystem))]
public sealed partial class StoriesHereticVoidIceComponent : Component
{
    [DataField]
    public TimeSpan WarmMeltTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan ColdMeltTime = TimeSpan.FromSeconds(60);

    [DataField]
    public float WarmThreshold = 273.15f;

    [DataField, AutoPausedField]
    public TimeSpan EndTime;
}
