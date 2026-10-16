using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ash;

public sealed partial class StoriesHereticAshSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private StoriesHereticGraspSystem _grasp = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> AshPath = "STHereticPathAsh";

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticMarkTriggeredEvent>(OnMarkTriggered);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticVolcanoBlastEvent>(OnVolcanoBlast);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticRebirthEvent>(OnRebirth);
        SubscribeLocalEvent<StoriesHereticBladeFireComponent, StoriesHereticBladeHitEvent>(OnBladeFireHit);

        InitializeAscension();
        InitializeMantle();
    }

    private void Ignite(EntityUid target, float stacks)
    {
        if (TryComp<FlammableComponent>(target, out var flammable))
            _flammable.AdjustFireStacks(target, stacks, flammable, ignite: true);
    }

    private bool IsBurning(EntityUid uid)
    {
        return TryComp<FlammableComponent>(uid, out var flammable) && flammable.OnFire;
    }

    private void OnMarkTriggered(ref StoriesHereticMarkTriggeredEvent args)
    {
        if (args.Path != AshPath || !TryComp<StoriesHereticAshMarkComponent>(args.Heretic, out var config))
            return;

        _stamina.TakeStaminaDamage(args.Target, config.StaminaDamage, source: args.Heretic);
        Ignite(args.Target, config.BaseFireStacks + args.Hops);

        if (args.Hops >= config.MaxHops)
            return;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(args.Target).Coordinates, config.SpreadRadius, _mobs);
        foreach (var mob in _mobs)
        {
            if (mob.Owner == args.Target || HasComp<StoriesHereticComponent>(mob))
                continue;

            _grasp.ApplyMark(mob, AshPath, config.SpreadMarkDuration, args.Hops + 1);
        }
    }

    private void OnVolcanoBlast(Entity<StoriesHereticComponent> ent, ref StoriesHereticVolcanoBlastEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var current = ent.Owner;
        var visited = new HashSet<EntityUid> { ent.Owner };
        for (var i = 0; i < args.Bounces; i++)
        {
            if (FindNearestUnburnt(current, args.Range, visited) is not { } next)
                break;

            visited.Add(next);
            _damageable.TryChangeDamage(next, args.Damage, origin: ent);
            Ignite(next, args.FireStacks);
            _damageable.TryChangeDamage(ent.Owner, args.SelfHeal, true);
            current = next;
        }
    }

    private EntityUid? FindNearestUnburnt(EntityUid from, float range, HashSet<EntityUid> exclude)
    {
        var origin = _transform.GetMapCoordinates(from).Position;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(from).Coordinates, range, _mobs);

        EntityUid? best = null;
        var bestDistance = float.MaxValue;
        foreach (var mob in _mobs)
        {
            if (exclude.Contains(mob)
                || HasComp<StoriesHereticComponent>(mob)
                || !_mobState.IsAlive(mob)
                || IsBurning(mob))
                continue;

            var distance = (_transform.GetMapCoordinates(mob).Position - origin).LengthSquared();
            if (distance >= bestDistance)
                continue;

            best = mob;
            bestDistance = distance;
        }

        return best;
    }

    private void OnRebirth(Entity<StoriesHereticComponent> ent, ref StoriesHereticRebirthEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, args.Range, _mobs);

        var drained = 0;
        foreach (var mob in _mobs)
        {
            if (mob.Owner == ent.Owner || !IsBurning(mob) || _mobState.IsDead(mob))
                continue;

            drained++;
            _damageable.TryChangeDamage(ent.Owner, args.HealPerTarget, true);

            if (_mobState.IsCritical(mob))
                _damageable.TryChangeDamage(mob.Owner, args.FinishDamage, true, origin: ent);
        }

        _flammable.Extinguish(ent);
        _popup.PopupEntity(Loc.GetString("stories-heretic-ash-rebirth", ("count", drained)), ent, ent);
    }

    private void OnBladeFireHit(Entity<StoriesHereticBladeFireComponent> ent, ref StoriesHereticBladeHitEvent args)
    {
        Ignite(args.Target, ent.Comp.FireStacks);
    }
}
