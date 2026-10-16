using System.Linq;
using System.Numerics;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared._Stories.Holy;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticGraspSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private SharedHolySystem _holy = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private static readonly EntProtoId SlurredEffect = "StatusEffectSlurred";
    private static readonly EntProtoId BlindnessEffect = "StatusEffectBlindness";
    private const int TeleportAttempts = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticGraspEvent>(OnGrasp);
        SubscribeLocalEvent<StoriesHereticGraspBlindComponent, StoriesHereticGraspHitEvent>(OnGraspBlind);
        SubscribeLocalEvent<StoriesHereticGraspMarkComponent, StoriesHereticGraspHitEvent>(OnGraspMark);
        SubscribeLocalEvent<StoriesHereticBladeComponent, MeleeHitEvent>(OnBladeHit);
        SubscribeLocalEvent<StoriesHereticBladeComponent, UseInHandEvent>(OnBladeUse);
        SubscribeLocalEvent<StoriesHereticBladeComponent, StoriesHereticBladeBreakDoAfterEvent>(OnBladeBreak);
        SubscribeLocalEvent<StoriesHereticSacrificedComponent, AttackedEvent>(OnSacrificedAttacked);
        SubscribeLocalEvent<StoriesHereticComponent, AttackedEvent>(OnHereticAttacked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticMarkComponent>();
        while (query.MoveNext(out var uid, out var mark))
        {
            if (now >= mark.EndTime)
                RemCompDeferred<StoriesHereticMarkComponent>(uid);
        }
    }

    private void OnGrasp(Entity<StoriesHereticComponent> ent, ref StoriesHereticGraspEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (HasComp<StoriesHereticRuneComponent>(target))
        {
            args.Handled = true;
            if (_net.IsServer)
                QueueDel(target);

            return;
        }

        if (target == ent.Owner || !HasComp<MobStateComponent>(target))
            return;

        args.Handled = true;

        if (IsProtected(target))
        {
            _popup.PopupPredicted(Loc.GetString("stories-heretic-grasp-blocked"), target, ent);
            return;
        }

        _stamina.TakeStaminaDamage(target, args.StaminaDamage, source: ent);
        _stun.TryKnockdown(target, args.KnockdownTime);
        _statusEffects.TryUpdateStatusEffectDuration(target, SlurredEffect, args.SlurTime);

        var ev = new StoriesHereticGraspHitEvent(ent, target);
        RaiseLocalEvent(ent, ref ev);
    }

    private bool IsProtected(EntityUid target)
    {
        return _holy.IsHoly(target) || _hands.EnumerateHeld(target).Any(HasComp<HolyComponent>);
    }

    private void OnGraspBlind(Entity<StoriesHereticGraspBlindComponent> ent, ref StoriesHereticGraspHitEvent args)
    {
        _statusEffects.TryUpdateStatusEffectDuration(args.Target, BlindnessEffect, ent.Comp.Duration);
    }

    private void OnGraspMark(Entity<StoriesHereticGraspMarkComponent> ent, ref StoriesHereticGraspHitEvent args)
    {
        ApplyMark(args.Target, ent.Comp.Path, ent.Comp.Duration, 0);
    }

    public void ApplyMark(EntityUid target, ProtoId<TagPrototype> path, TimeSpan duration, int hops)
    {
        var mark = EnsureComp<StoriesHereticMarkComponent>(target);
        mark.Path = path;
        mark.EndTime = _timing.CurTime + duration;
        mark.Hops = hops;
        Dirty(target, mark);
    }

    private void OnBladeHit(Entity<StoriesHereticBladeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !TryComp<StoriesHereticComponent>(args.User, out var heretic))
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == args.User)
                continue;

            var hit = new StoriesHereticBladeHitEvent(args.User, target, ent);
            RaiseLocalEvent(args.User, ref hit);

            if (!TryComp<StoriesHereticMarkComponent>(target, out var mark) || mark.Path != heretic.Path)
                continue;

            var triggered = new StoriesHereticMarkTriggeredEvent(args.User, target, mark.Path, mark.Hops);
            RemComp(target, mark);
            RaiseLocalEvent(ref triggered);
        }
    }

    private void OnBladeUse(Entity<StoriesHereticBladeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !HasComp<StoriesHereticComponent>(args.User))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.BreakTime,
            new StoriesHereticBladeBreakDoAfterEvent(),
            ent,
            used: ent)
        {
            BreakOnMove = false,
            NeedHand = true,
        });
    }

    private void OnBladeBreak(Entity<StoriesHereticBladeComponent> ent, ref StoriesHereticBladeBreakDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        var user = args.User;
        var origin = _transform.GetMapCoordinates(user);
        for (var i = 0; i < TeleportAttempts; i++)
        {
            var distance = _random.NextFloat(ent.Comp.TeleportMin, ent.Comp.TeleportMax);
            var offset = _random.NextAngle().ToVec() * distance;
            var coords = _transform.ToCoordinates(new MapCoordinates(origin.Position + offset, origin.MapId));

            if (_turf.GetTileRef(coords) is not { } tile
                || _turf.IsSpace(tile)
                || _turf.IsTileBlocked(tile, CollisionGroup.Impassable))
                continue;

            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):user} teleported with heretic blade {ToPrettyString(ent):blade}");
            _audio.PlayPvs(ent.Comp.BreakSound, user);
            _transform.SetCoordinates(user, coords);
            _transform.AttachToGridOrMap(user);
            QueueDel(ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString("stories-heretic-blade-no-place"), user, user);
    }

    private void OnSacrificedAttacked(Entity<StoriesHereticSacrificedComponent> ent, ref AttackedEvent args)
    {
        if (HasComp<StoriesHereticComponent>(args.User) && _heretic.HasFocus(args.User))
            Punish(ent);
    }

    private void OnHereticAttacked(Entity<StoriesHereticComponent> ent, ref AttackedEvent args)
    {
        if (TryComp<StoriesHereticSacrificedComponent>(args.User, out var sacrificed) && _heretic.HasFocus(ent))
            Punish((args.User, sacrificed));
    }

    private void Punish(Entity<StoriesHereticSacrificedComponent> ent)
    {
        _stun.TryKnockdown(ent.Owner, ent.Comp.KnockdownTime);
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, true);
    }
}
