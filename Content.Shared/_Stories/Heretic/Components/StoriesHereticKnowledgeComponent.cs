using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
    /// Иконка знания в дереве поверх печати пути. <c>null</c> — только печать.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon;

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

    /// <summary>
    /// Знание только для последователей своего пути (Хватка, Метка, улучшение клинка).
    /// Корень (<see cref="ChoosesPath"/>) и <see cref="Ascension"/> закрыты для чужих путей всегда.
    /// Остальные знания пути доступны с чужого пути через мосты — как в дереве Paradise.
    /// </summary>
    [DataField]
    public bool PathLocked;

    /// <summary>
    /// Побочное знание между двумя соседними путями кольца. Только для раскладки окна: доступ задаёт <see cref="Requires"/>.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> Bridge = new();

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
