using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Stories.Heretic.Cloak;

public sealed partial class StoriesHereticShadowCloakSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<TagPrototype> FootstepTag = "FootstepSound";
    private static readonly EntProtoId SlowdownEffect = "STHereticShadowCloakSlowdownStatusEffect";
    private static readonly SoundSpecifier ActivateSound = new SoundCollectionSpecifier("STHereticCurse");

    public override void Initialize()
    {
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticShadowCloakEvent>(OnCloakAction);
        SubscribeLocalEvent<StoriesHereticShadowCloakComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoriesHereticShadowCloakComponent, TransformSpeakerNameEvent>(OnSpeakerName);
        SubscribeLocalEvent<StoriesHereticShadowCloakComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<StoriesHereticShadowCloakComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<StoriesHereticShadowCloakComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<StoriesHereticShadowCloakComponent, SleepStateChangedEvent>(OnSleep);
    }

    private void OnCloakAction(Entity<StoriesHereticComponent> ent, ref StoriesHereticShadowCloakEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var uid = ent.Owner;
        EntityUid? action = args.Action.Owner.Valid ? args.Action.Owner : null;

        // Повторное нажатие снимает плащ вручную.
        if (TryComp<StoriesHereticShadowCloakComponent>(uid, out var active))
        {
            var remaining = active.EndTime - _timing.CurTime;
            var cooldown = remaining > active.MinCooldown ? remaining : active.MinCooldown;

            RemCompDeferred<StoriesHereticShadowCloakComponent>(uid);
            _popup.PopupClient(Loc.GetString("stories-heretic-cloak-off"), uid, uid);
            // PerformAction после обработчика сбрасывает перезарядку и ставит UseDelay, поэтому задержка задаётся через него.
            _actions.SetUseDelay(active.Action ?? action, cooldown);
            return;
        }

        _actions.SetUseDelay(action, args.MinCooldown);

        var cloak = new StoriesHereticShadowCloakComponent
        {
            SpeedMultiplier = args.SpeedMultiplier,
            EndTime = _timing.CurTime + args.Duration,
            DamageThreshold = args.DamageThreshold,
            Action = action,
            MinCooldown = args.MinCooldown,
            InterruptCooldown = args.InterruptCooldown,
            InterruptKnockdown = args.InterruptKnockdown,
            InterruptSlowdown = args.InterruptSlowdown,
            InterruptSlowTime = args.InterruptSlowTime,
        };

        cloak.HadIdentityBlocker = HasComp<IdentityBlockerComponent>(uid);
        if (!cloak.HadIdentityBlocker)
            AddComp(uid, new IdentityBlockerComponent { Coverage = IdentityBlockerCoverage.FULL });

        cloak.HadFootstepTag = _tag.HasTag(uid, FootstepTag);
        if (cloak.HadFootstepTag)
            _tag.RemoveTag(uid, FootstepTag);

        cloak.Smoke = PredictedSpawnAttachedTo(args.Smoke, new EntityCoordinates(uid, default));

        AddComp(uid, cloak);
        _identity.QueueIdentityUpdate(uid);
        _speed.RefreshMovementSpeedModifiers(uid);

        _audio.PlayPredicted(ActivateSound, uid, uid);
        _popup.PopupPredicted(Loc.GetString("stories-heretic-cloak-on"),
            Loc.GetString("stories-heretic-cloak-on-others", ("ent", uid)),
            uid,
            uid);
    }

    /// <summary>
    /// Снятие плаща любым путём восстанавливает всё запомненное.
    /// </summary>
    private void OnShutdown(Entity<StoriesHereticShadowCloakComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var uid = ent.Owner;
        if (!ent.Comp.HadIdentityBlocker)
            RemComp<IdentityBlockerComponent>(uid);

        if (ent.Comp.HadFootstepTag)
            _tag.AddTag(uid, FootstepTag);

        if (ent.Comp.Smoke is { } smoke)
            PredictedQueueDel(smoke);

        _identity.QueueIdentityUpdate(uid);
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnSpeakerName(Entity<StoriesHereticShadowCloakComponent> ent, ref TransformSpeakerNameEvent args)
    {
        args.VoiceName = Loc.GetString("stories-heretic-cloak-voice");
    }

    private void OnRefreshSpeed(Entity<StoriesHereticShadowCloakComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
            return;

        args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }

    private void OnDamage(Entity<StoriesHereticShadowCloakComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null || _net.IsClient)
            return;

        foreach (var value in args.DamageDelta.DamageDict.Values)
        {
            if (value > 0)
                ent.Comp.DamageTaken += (float) value;
        }

        if (ent.Comp.DamageTaken < ent.Comp.DamageThreshold)
            return;

        if (_random.Prob(Math.Min(1f, ent.Comp.DamageTaken / 100f)))
            Break(ent);
    }

    private void OnMobState(Entity<StoriesHereticShadowCloakComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive && !_net.IsClient)
            Break(ent);
    }

    private void OnSleep(Entity<StoriesHereticShadowCloakComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep && !_net.IsClient)
            Break(ent);
    }

    /// <summary>
    /// Срыв плаща: нокдаун, замедление и долгая перезарядка.
    /// </summary>
    private void Break(Entity<StoriesHereticShadowCloakComponent> ent)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        // Немедленно, чтобы второй удар в этом же тике не сорвал плащ повторно.
        RemComp<StoriesHereticShadowCloakComponent>(uid);

        _stun.TryKnockdown(uid, comp.InterruptKnockdown, true);
        _movementMod.TryUpdateMovementSpeedModDuration(uid, SlowdownEffect, comp.InterruptSlowTime, comp.InterruptSlowdown);
        SetCooldown(comp.Action, comp.InterruptCooldown);
        _popup.PopupEntity(Loc.GetString("stories-heretic-cloak-broken"), uid, uid, PopupType.MediumCaution);
    }

    private void SetCooldown(EntityUid? action, TimeSpan cooldown)
    {
        if (action != null)
            _actions.SetCooldown(action.Value, cooldown);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<StoriesHereticShadowCloakComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;

            RemCompDeferred<StoriesHereticShadowCloakComponent>(uid);
            _popup.PopupEntity(Loc.GetString("stories-heretic-cloak-off"), uid, uid);
            SetCooldown(comp.Action, comp.MinCooldown);
        }
    }
}
