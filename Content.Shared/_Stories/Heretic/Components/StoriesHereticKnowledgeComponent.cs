using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Данные одного знания. Живёт только на прототипах <c>STHereticKnowledge*</c>, сущности не спавнятся.
/// </summary>
[RegisterComponent]
public sealed partial class StoriesHereticKnowledgeComponent : Component
{
    [DataField]
    public int Cost = 1;

    /// <summary>
    /// Путь знания. <c>null</c> — общее или побочное знание.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype>? Path;

    [DataField]
    public int Stage;

    /// <summary>
    /// Нужно знать хотя бы одно из перечисленных (ИЛИ). Пустой список — без требований.
    /// </summary>
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

    /// <summary>
    /// Очки, выдаваемые при изучении (Пир сов).
    /// </summary>
    [DataField]
    public int GrantPoints;

    /// <summary>
    /// Изучение навсегда закрывает Вознесение (Пир сов).
    /// </summary>
    [DataField]
    public bool LocksAscension;
}
