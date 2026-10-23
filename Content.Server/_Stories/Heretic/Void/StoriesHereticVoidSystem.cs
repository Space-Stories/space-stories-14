using Content.Server.Body.Components;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared._Stories.Heretic.Void;
using Content.Shared.StatusEffectNew;
using Content.Shared.Tag;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidSystem : EntitySystem
{
    [Dependency] private StoriesHereticVoidChillSystem _chill = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;

    private static readonly EntProtoId MutedEffect = "StatusEffectMuted";
    private static readonly ProtoId<TagPrototype> VoidPath = "STHereticPathVoid";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticMarkTriggeredEvent>(OnMarkTriggered);
        SubscribeLocalEvent<StoriesHereticGraspVoidComponent, StoriesHereticGraspHitEvent>(OnGraspVoid);
        SubscribeLocalEvent<StoriesHereticColdImmuneComponent, ComponentStartup>(OnColdImmuneStartup);
        SubscribeLocalEvent<StoriesHereticColdImmuneComponent, ComponentShutdown>(OnColdImmuneShutdown);

        InitializeBlast();
        InitializePhase();
        InitializePull();
        InitializeSeeking();
    }

    private void OnGraspVoid(Entity<StoriesHereticGraspVoidComponent> ent, ref StoriesHereticGraspHitEvent args)
    {
        _statusEffects.TryUpdateStatusEffectDuration(args.Target, MutedEffect, ent.Comp.MuteTime);
        _chill.ApplyChill(args.Target, ent.Comp.ChillStacks);
    }

    private void OnMarkTriggered(ref StoriesHereticMarkTriggeredEvent args)
    {
        if (args.Path != VoidPath || !TryComp<StoriesHereticVoidMarkComponent>(args.Heretic, out var config))
            return;

        if (_chill.IsImmune(args.Target))
            return;

        _chill.ApplyChill(args.Target, config.ChillStacks);

        if (TryComp<TemperatureComponent>(args.Target, out var temperature))
            _temperature.ForceChangeTemperature((args.Target, temperature), temperature.Temperature - config.TemperatureDrop);
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
