using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Stories.Heretic.Organs;

public sealed partial class StoriesHereticCorruptedOrganSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private OrganRelationSystem _relations = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;

    private static readonly ProtoId<ReagentPrototype> HolyWater = "Holywater";

    /// <summary>
    /// Категория штатного органа → испорченная замена.
    /// </summary>
    private static readonly (ProtoId<OrganCategoryPrototype> Category, EntProtoId Corrupted)[] Replacements =
    [
        ("Eyes", "STHereticCorruptedEyes"),
        ("Tongue", "STHereticCorruptedTongue"),
        ("Liver", "STHereticCorruptedLiver"),
        ("Stomach", "STHereticCorruptedStomach"),
        ("Heart", "STHereticCorruptedHeart"),
        ("Lungs", "STHereticCorruptedLungs"),
        ("Appendix", "STHereticCorruptedAppendix"),
    ];

    private readonly List<(EntityUid Organ, EntProtoId Corrupted)> _candidates = new();

    /// <summary>
    /// Заменяет 2–4 случайных органа на испорченные, старые выпадают в <paramref name="dropAt"/>.
    /// </summary>
    public int CurseOrgans(EntityUid victim, EntityCoordinates dropAt)
    {
        if (!_containers.TryGetContainer(victim, BodyComponent.ContainerID, out var container))
            return 0;

        _candidates.Clear();
        foreach (var organ in container.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var comp) || HasComp<StoriesHereticCorruptedOrganComponent>(organ))
                continue;

            foreach (var (category, corrupted) in Replacements)
            {
                if (comp.Category == category)
                    _candidates.Add((organ, corrupted));
            }
        }

        _random.Shuffle(_candidates);
        var count = Math.Min(_random.Next(2, 5), _candidates.Count);
        for (var i = 0; i < count; i++)
        {
            Replace(container, _candidates[i].Organ, _candidates[i].Corrupted, dropAt);
        }

        if (count > 0)
            EnsureComp<StoriesHereticCorruptedBodyComponent>(victim);

        return count;
    }

    /// <summary>
    /// Святая вода в крови подавляет эффекты испорченных органов.
    /// </summary>
    public bool IsSuppressed(EntityUid body)
    {
        return _solutions.TryGetSolution(body, BloodstreamComponent.DefaultBloodSolutionName, out _, out var blood)
               && blood.GetReagentQuantity(new(HolyWater, null)) > 0;
    }

    private void Replace(BaseContainer container, EntityUid old, EntProtoId corrupted, EntityCoordinates dropAt)
    {
        var parent = CompOrNull<ChildOrganComponent>(old)?.Parent;
        // Старый орган остаётся ребёнком родителя после извлечения — разрываем связь явно.
        _relations.Orphan(old);
        _containers.Remove(old, container, destination: dropAt);

        var organ = Spawn(corrupted, dropAt);
        _containers.Insert(organ, container);
        if (parent != null && HasComp<ChildOrganComponent>(organ))
            _relations.Relate(parent.Value, organ);
    }
}
