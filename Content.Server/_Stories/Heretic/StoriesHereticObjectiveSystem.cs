using System.Diagnostics.CodeAnalysis;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticObjectiveSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private RoleSystem _roles = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticObjectiveConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StoriesHereticObjectiveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<StoriesHereticComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindRemoved(Entity<StoriesHereticComponent> ent, ref MindRemovedMessage args)
    {
        if (_roles.MindHasRole<StoriesHereticRoleComponent>(args.Mind.AsNullable(), out var role))
            Save(role.Value.Comp2, ent.Comp);
    }

    private void OnAfterAssign(Entity<StoriesHereticObjectiveConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (TryGetProgress(args.MindId, args.Mind, out var progress))
            SetTitle(ent, progress, args.Meta);
    }

    private void OnGetProgress(Entity<StoriesHereticObjectiveConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryGetProgress(args.MindId, args.Mind, out var progress))
        {
            args.Progress = 0f;
            return;
        }

        var (current, required) = GetCounts(ent.Comp.Kind, progress);
        args.Progress = required <= 0 ? 1f : Math.Clamp((float) current / required, 0f, 1f);
    }

    public bool TryGetProgress(EntityUid mindId, MindComponent mind, [NotNullWhen(true)] out StoriesHereticRoleComponent? progress)
    {
        progress = null;
        if (!_roles.MindHasRole<StoriesHereticRoleComponent>((mindId, mind), out var role))
            return false;

        progress = role.Value.Comp2;
        if (mind.OwnedEntity is { } body && TryComp<StoriesHereticComponent>(body, out var heretic))
            Save(progress, heretic);

        return true;
    }

    public void RefreshTitles(EntityUid mindId, MindComponent mind)
    {
        if (!TryGetProgress(mindId, mind, out var progress))
            return;

        foreach (var objective in mind.Objectives)
        {
            if (TryComp<StoriesHereticObjectiveConditionComponent>(objective, out var condition))
                SetTitle((objective, condition), progress);
        }
    }

    private void SetTitle(Entity<StoriesHereticObjectiveConditionComponent> ent, StoriesHereticRoleComponent progress, MetaDataComponent? meta = null)
    {
        var (_, required) = GetCounts(ent.Comp.Kind, progress);
        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("count", required)), meta);
    }

    private static void Save(StoriesHereticRoleComponent progress, StoriesHereticComponent heretic)
    {
        progress.Path = heretic.Path;
        progress.LearnedCount = heretic.LearnedCount;
        progress.SacrificeCount = heretic.SacrificeCount;
        progress.HeadSacrificeCount = heretic.HeadSacrificeCount;
        progress.RequiredKnowledge = heretic.RequiredKnowledge;
        progress.RequiredSacrifices = heretic.RequiredSacrifices;
        progress.RequiredHeadSacrifices = heretic.RequiredHeadSacrifices;
        progress.Ascended = heretic.Ascended;
    }

    private static (int Current, int Required) GetCounts(StoriesHereticObjectiveKind kind, StoriesHereticRoleComponent progress)
    {
        return kind switch
        {
            StoriesHereticObjectiveKind.Knowledge => (progress.LearnedCount, progress.RequiredKnowledge),
            StoriesHereticObjectiveKind.Sacrifice => (progress.SacrificeCount, progress.RequiredSacrifices),
            StoriesHereticObjectiveKind.HeadSacrifice => (progress.HeadSacrificeCount, progress.RequiredHeadSacrifices),
            _ => (0, 0),
        };
    }
}
