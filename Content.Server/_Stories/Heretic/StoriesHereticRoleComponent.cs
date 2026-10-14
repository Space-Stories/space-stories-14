using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic;

/// <summary>
/// Роль еретика на разуме. Хранит снимок прогресса, чтобы цели и итоги раунда не терялись,
/// когда разум уходит из тела еретика (гиб, борг, клон, крио).
/// </summary>
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
    public int SummonCount;

    [DataField]
    public int RequiredSummons;

    [DataField]
    public bool Ascended;
}
