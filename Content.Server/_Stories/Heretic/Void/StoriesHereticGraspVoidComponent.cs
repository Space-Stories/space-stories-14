namespace Content.Server._Stories.Heretic.Void;

[RegisterComponent, Access(typeof(StoriesHereticVoidSystem))]
public sealed partial class StoriesHereticGraspVoidComponent : Component
{
    [DataField]
    public TimeSpan MuteTime = TimeSpan.FromSeconds(8);

    [DataField]
    public int ChillStacks = 2;
}
