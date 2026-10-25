namespace Content.Server._Stories.Heretic.Void;

[RegisterComponent, Access(typeof(StoriesHereticVoidSystem))]
public sealed partial class StoriesHereticVoidMarkComponent : Component
{
    [DataField]
    public int ChillStacks = 3;

    [DataField]
    public float TemperatureDrop = 40f;
}
