using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticFleshAscensionRitualComponent : Component
{
    [DataField]
    public int Corpses = 4;

    [DataField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public LocId Announcement = "stories-heretic-flesh-ascension-announcement";

    [DataField]
    public Color AnnouncementColor = Color.DarkRed;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_Stories/Heretic/ascend_flesh.ogg");
}
