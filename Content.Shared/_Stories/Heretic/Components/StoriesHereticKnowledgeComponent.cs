using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent]
public sealed partial class StoriesHereticKnowledgeComponent : Component
{
    [DataField]
    public int Cost = 1;

    [DataField]
    public ProtoId<TagPrototype>? Path;

    [DataField]
    public int Stage;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public List<EntProtoId<StoriesHereticKnowledgeComponent>> Requires = new();

    [DataField]
    public bool ChoosesPath;

    [DataField]
    public bool Starting;

    [DataField]
    public bool Ascension;

    [DataField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public List<EntProtoId<StoriesHereticRitualComponent>> Rituals = new();

    [DataField]
    public ComponentRegistry AddComponents = new();

    [DataField]
    public int GrantPoints;

    [DataField]
    public bool LocksAscension;

    [DataField]
    public bool Side;
}
