using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server._Stories.Heretic;

/// <summary>
/// Прогресс целей еретика. Требуемые числа хранятся в <see cref="StoriesHereticComponent"/>, чтобы не дублировать их.
/// </summary>
public sealed partial class StoriesHereticObjectiveSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticObjectiveConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StoriesHereticObjectiveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAfterAssign(Entity<StoriesHereticObjectiveConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!TryGetHeretic(args.Mind, out var heretic))
            return;

        var (_, required) = GetCounts(ent.Comp.Kind, heretic);
        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("count", required)), args.Meta);
    }

    private void OnGetProgress(Entity<StoriesHereticObjectiveConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryGetHeretic(args.Mind, out var heretic))
        {
            args.Progress = 0f;
            return;
        }

        var (current, required) = GetCounts(ent.Comp.Kind, heretic);
        args.Progress = required <= 0 ? 1f : Math.Clamp((float) current / required, 0f, 1f);
    }

    private bool TryGetHeretic(MindComponent mind, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out StoriesHereticComponent? heretic)
    {
        heretic = null;
        return mind.OwnedEntity is { } body && TryComp(body, out heretic);
    }

    private static (int Current, int Required) GetCounts(StoriesHereticObjectiveKind kind, StoriesHereticComponent heretic)
    {
        return kind switch
        {
            StoriesHereticObjectiveKind.Knowledge => (heretic.LearnedCount, heretic.RequiredKnowledge),
            StoriesHereticObjectiveKind.Sacrifice => (heretic.SacrificeCount, heretic.RequiredSacrifices),
            StoriesHereticObjectiveKind.HeadSacrifice => (heretic.HeadSacrificeCount, heretic.RequiredHeadSacrifices),
            _ => (0, 0),
        };
    }
}
