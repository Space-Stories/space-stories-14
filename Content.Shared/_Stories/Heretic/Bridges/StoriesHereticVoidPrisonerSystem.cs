using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Speech;

namespace Content.Shared._Stories.Heretic.Bridges;

public sealed partial class StoriesHereticVoidPrisonerSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, BeforeStaminaDamageEvent>(OnBeforeStamina);
        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<StoriesHereticVoidPrisonerComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<ActionComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnStartup(Entity<StoriesHereticVoidPrisonerComponent> ent, ref ComponentStartup args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnShutdown(Entity<StoriesHereticVoidPrisonerComponent> ent, ref ComponentShutdown args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnBeforeDamage(Entity<StoriesHereticVoidPrisonerComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    private void OnBeforeStamina(Entity<StoriesHereticVoidPrisonerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInteractionAttempt(Entity<StoriesHereticVoidPrisonerComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnUpdateCanMove(Entity<StoriesHereticVoidPrisonerComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnAttackAttempt(Entity<StoriesHereticVoidPrisonerComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnUseAttempt(Entity<StoriesHereticVoidPrisonerComponent> ent, ref UseAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnSpeakAttempt(Entity<StoriesHereticVoidPrisonerComponent> ent, ref SpeakAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnActionAttempt(Entity<ActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled || !HasComp<StoriesHereticVoidPrisonerComponent>(args.User))
            return;

        args.Cancelled = true;
    }
}
