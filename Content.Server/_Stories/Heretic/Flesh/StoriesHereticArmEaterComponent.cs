using Content.Shared.Body;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// Владыка Ночи съедает отделённые руки, на которые нажимает, и лечится.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticArmEaterComponent : Component
{
    /// <summary>
    /// Доля максимума здоровья, которую лечит одна рука (по всем имеющимся видам урона).
    /// </summary>
    [DataField]
    public float HealFraction = 0.5f;

    /// <summary>
    /// Отделённая часть тела считается рукой, если в ней есть орган одной из этих категорий.
    /// </summary>
    [DataField]
    public List<ProtoId<OrganCategoryPrototype>> Categories = new() { "ArmLeft", "ArmRight", "HandLeft", "HandRight" };

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");
}
