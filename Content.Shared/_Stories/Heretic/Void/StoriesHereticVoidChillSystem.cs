using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidChillSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private StoriesHereticGraspSystem _grasp = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<AlertPrototype> ChillAlert = "STHereticVoidChill";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticVoidChillComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<StoriesHereticVoidChillComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticVoidChillComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var chill, out var temp))
        {
            if (now >= chill.EndTime)
            {
                RemCompDeferred<StoriesHereticVoidChillComponent>(uid);
                continue;
            }

            if (now < chill.NextTick)
                continue;

            chill.NextTick = now + chill.TickInterval;

            if (temp.Temperature <= chill.MinTemperature)
                continue;

            var target = MathF.Max(chill.MinTemperature, temp.Temperature - chill.CoolingPerStack * chill.Stacks);
            _temperature.ForceChangeTemperature((uid, temp), target);
        }
    }

    public bool IsImmune(EntityUid target)
    {
        return HasComp<StoriesHereticComponent>(target)
               || _heretic.IsServant(target)
               || _grasp.IsProtected(target)
               || !HasComp<TemperatureComponent>(target)
               || _mobState.IsDead(target);
    }

    public void ApplyChill(EntityUid target, int stacks)
    {
        if (IsImmune(target))
            return;

        var chill = EnsureComp<StoriesHereticVoidChillComponent>(target);
        chill.Stacks = Math.Min(chill.Stacks + stacks, chill.MaxStacks);
        chill.EndTime = _timing.CurTime + chill.Duration;
        Dirty(target, chill);

        _speed.RefreshMovementSpeedModifiers(target);
        _alerts.ShowAlert(target, ChillAlert, (short) chill.Stacks);
    }

    private void OnRefreshSpeed(Entity<StoriesHereticVoidChillComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(Math.Max(0.1f, 1f - ent.Comp.SlowPerStack * ent.Comp.Stacks));
    }

    private void OnShutdown(Entity<StoriesHereticVoidChillComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, ChillAlert);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }
}
