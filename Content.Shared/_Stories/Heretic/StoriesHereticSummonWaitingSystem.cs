using Content.Shared._Stories.Heretic.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticSummonWaitingSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticSummonWaitingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<StoriesHereticSummonWaitingComponent, AttackAttemptEvent>(OnAttack);
        SubscribeLocalEvent<StoriesHereticSummonWaitingComponent, InteractionAttemptEvent>(OnInteract);
        SubscribeLocalEvent<StoriesHereticSummonWaitingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoriesHereticSummonWaitingComponent, ComponentShutdown>(OnShutdown);
    }

    // Не-ref события подписываются только в форме (uid, comp, args).
    private void OnUpdateCanMove(EntityUid uid, StoriesHereticSummonWaitingComponent comp, UpdateCanMoveEvent args)
    {
        if (comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnAttack(EntityUid uid, StoriesHereticSummonWaitingComponent comp, AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnInteract(Entity<StoriesHereticSummonWaitingComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStartup(Entity<StoriesHereticSummonWaitingComponent> ent, ref ComponentStartup args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnShutdown(Entity<StoriesHereticSummonWaitingComponent> ent, ref ComponentShutdown args)
    {
        _blocker.UpdateCanMove(ent);
    }
}
