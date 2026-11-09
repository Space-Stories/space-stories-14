using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Void;

[RegisterComponent, Access(typeof(StoriesHereticVoidSystem))]
public sealed partial class StoriesHereticVoidAscensionRitualComponent : Component
{
    [DataField]
    public int Corpses = 3;

    [DataField]
    public ComponentRegistry AddComponents = new();

    [DataField]
    public bool RemoveBreathing = true;

    [DataField]
    public LocId Announcement = "stories-heretic-void-ascension-announcement";

    [DataField]
    public Color AnnouncementColor = Color.LightSkyBlue;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Stories/Heretic/ascend_void.ogg");

    [DataField]
    public SoundSpecifier Music = new SoundPathSpecifier("/Audio/_Stories/Heretic/VoidsEmbrace.ogg");

    [DataField]
    public float MusicGain = 0.6f;
}
