using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic;

[RegisterComponent, Access(typeof(StoriesHereticObjectiveSystem))]
public sealed partial class StoriesHereticRoleComponent : Component
{
    [DataField]
    public ProtoId<TagPrototype>? Path;

    [DataField]
    public int LearnedCount;

    [DataField]
    public int SacrificeCount;

    [DataField]
    public int HeadSacrificeCount;

    [DataField]
    public int RequiredKnowledge;

    [DataField]
    public int RequiredSacrifices;

    [DataField]
    public int RequiredHeadSacrifices;

    [DataField]
    public bool Ascended;
}
