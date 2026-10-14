using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// Данные ритуала «Последний гимн жреца» (Вознесение Плоти). Живёт на прототипе ритуала.
/// Лимиты гулей и клинков после Вознесения задаются у самих Хватки, ритуалов Безгласных и клинка.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticFleshAscensionRitualComponent : Component
{
    [DataField]
    public int Corpses = 4;

    /// <summary>
    /// Действия вознёсшегося: «Сбросить форму».
    /// </summary>
    [DataField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public LocId Announcement = "stories-heretic-flesh-ascension-announcement";

    [DataField]
    public Color AnnouncementColor = Color.DarkRed;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_Stories/Heretic/ascend_flesh.ogg");
}
