using Content.Server.Body.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared._Stories.Heretic.Void;
using Content.Shared.StatusEffectNew;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidSystem : EntitySystem
{
    [Dependency] private StoriesHereticVoidChillSystem _chill = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly EntProtoId MutedEffect = "StatusEffectMuted";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticGraspVoidComponent, StoriesHereticGraspHitEvent>(OnGraspVoid);
        SubscribeLocalEvent<StoriesHereticColdImmuneComponent, ComponentStartup>(OnColdImmuneStartup);
        SubscribeLocalEvent<StoriesHereticColdImmuneComponent, ComponentShutdown>(OnColdImmuneShutdown);
    }

    private void OnGraspVoid(Entity<StoriesHereticGraspVoidComponent> ent, ref StoriesHereticGraspHitEvent args)
    {
        _statusEffects.TryUpdateStatusEffectDuration(args.Target, MutedEffect, ent.Comp.MuteTime);
        _chill.ApplyChill(args.Target, ent.Comp.ChillStacks);
    }

    private void OnColdImmuneStartup(Entity<StoriesHereticColdImmuneComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<TemperatureDamageComponent>(ent, out var damage))
        {
            ent.Comp.OriginalThreshold ??= damage.ColdDamageThreshold;
            damage.ColdDamageThreshold = 0f;
        }

        if (ent.Comp.RemoveBreathing)
            RemComp<RespiratorComponent>(ent);
    }

    private void OnColdImmuneShutdown(Entity<StoriesHereticColdImmuneComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.OriginalThreshold is { } original && TryComp<TemperatureDamageComponent>(ent, out var damage))
            damage.ColdDamageThreshold = original;

        if (ent.Comp.RemoveBreathing && !TerminatingOrDeleted(ent))
            EnsureComp<RespiratorComponent>(ent);
    }
}
