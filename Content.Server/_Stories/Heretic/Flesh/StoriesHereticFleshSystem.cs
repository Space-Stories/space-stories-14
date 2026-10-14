using System.Linq;
using Content.Server._Stories.Heretic.Ghouls;
using Content.Server.Popups;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Flesh;

public sealed partial class StoriesHereticFleshSystem : EntitySystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StoriesHereticGhoulSystem _ghouls = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;

    private static readonly EntProtoId BlindnessEffect = "StatusEffectBlindness";
    private static readonly ProtoId<TagPrototype> FleshPath = "STHereticPathFlesh";

    public override void Initialize()
    {
        base.Initialize();

        InitializeKnit();
        InitializeLink();

        SubscribeLocalEvent<StoriesHereticGraspGhoulComponent, StoriesHereticGraspHitEvent>(OnGraspGhoul);
        SubscribeLocalEvent<StoriesHereticMarkTriggeredEvent>(OnMarkTriggered);
        SubscribeLocalEvent<StoriesHereticBladeBleedComponent, StoriesHereticBladeHitEvent>(OnBladeBleedHit);
        SubscribeLocalEvent<StoriesHereticFocusedAttackComponent, MeleeHitEvent>(OnFocusedHit);
        SubscribeLocalEvent<MobStateComponent, StoriesHereticEldritchBlindEvent>(OnEldritchBlind);
    }

    private void OnFocusedHit(Entity<StoriesHereticFocusedAttackComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var target = args.HitEntities.First();
        ent.Comp.Streak = target == ent.Comp.LastTarget ? ent.Comp.Streak + 1 : 0;
        ent.Comp.LastTarget = target;

        var bonus = Math.Min(ent.Comp.Bonus * ent.Comp.Streak, ent.Comp.MaxBonus);
        if (bonus > 0)
            args.BonusDamage.DamageDict["Blunt"] = args.BonusDamage.DamageDict.GetValueOrDefault("Blunt") + bonus;
    }

    private void OnEldritchBlind(Entity<MobStateComponent> ent, ref StoriesHereticEldritchBlindEvent args)
    {
        if (args.Handled || args.Target == ent.Owner || !HasComp<MobStateComponent>(args.Target))
            return;

        args.Handled = true;
        _statusEffects.TryUpdateStatusEffectDuration(args.Target, BlindnessEffect, args.Duration);
    }

    private void OnGraspGhoul(Entity<StoriesHereticGraspGhoulComponent> ent, ref StoriesHereticGraspHitEvent args)
    {
        // По живым Хватка работает как обычно, гулем становится только труп.
        if (!_mobState.IsDead(args.Target))
            return;

        var limit = ent.Comp.Limit + (TryComp<StoriesHereticComponent>(ent, out var heretic) && heretic.Ascended ? ent.Comp.AscendedBonus : 0);
        if (_ghouls.TryRaise(ent, args.Target, ent.Comp.Conversion, limit, out var reason))
            _popup.PopupEntity(Loc.GetString("stories-heretic-flesh-ghoul-raised", ("target", args.Target)), args.Target, ent);
        else
            _popup.PopupEntity(reason, args.Target, ent);
    }

    private void OnMarkTriggered(ref StoriesHereticMarkTriggeredEvent args)
    {
        if (args.Path != FleshPath || !TryComp<StoriesHereticFleshMarkComponent>(args.Heretic, out var config))
            return;

        _bloodstream.TryModifyBleedAmount(args.Target, config.Bleed);
        _damageable.TryChangeDamage(args.Target, config.Damage, origin: args.Heretic);
    }

    private void OnBladeBleedHit(Entity<StoriesHereticBladeBleedComponent> ent, ref StoriesHereticBladeHitEvent args)
    {
        if (_mobState.IsAlive(args.Target))
            _bloodstream.TryModifyBleedAmount(args.Target, ent.Comp.Bleed);
    }
}
