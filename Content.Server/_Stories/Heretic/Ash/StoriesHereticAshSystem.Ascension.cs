using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Ash;

public sealed partial class StoriesHereticAshSystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly EntProtoId HallucinationEffect = "StatusEffectSeeingRainbow";
    private static readonly EntProtoId PressureImmunityEffect = "StatusEffectPressureImmunity";

    private void InitializeAscension()
    {
        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticFireRingEvent>(OnFireRing);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticFireWaveEvent>(OnFireWave);
        SubscribeLocalEvent<StoriesHereticMadnessMaskComponent, GotEquippedEvent>(OnMaskEquipped);
        SubscribeLocalEvent<StoriesHereticMadnessMaskComponent, GotUnequippedEvent>(OnMaskUnequipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var rings = EntityQueryEnumerator<StoriesHereticFireRingComponent>();
        while (rings.MoveNext(out var uid, out var ring))
        {
            if (now >= ring.EndTime)
            {
                RemCompDeferred<StoriesHereticFireRingComponent>(uid);
                continue;
            }

            if (now < ring.NextPulse)
                continue;

            ring.NextPulse = now + ring.Interval;
            SpawnRingEffects((uid, ring));

            _mobs.Clear();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, ring.Radius, _mobs);
            foreach (var mob in _mobs)
            {
                if (mob.Owner != uid)
                    Ignite(mob, ring.FireStacks);
            }
        }

        var masks = EntityQueryEnumerator<StoriesHereticMadnessMaskComponent>();
        while (masks.MoveNext(out _, out var mask))
        {
            if (mask.Wearer is not { } wearer || now < mask.NextPulse || !HasComp<StoriesHereticComponent>(wearer))
                continue;

            mask.NextPulse = now + mask.Interval;
            _mobs.Clear();
            _lookup.GetEntitiesInRange(Transform(wearer).Coordinates, mask.Range, _mobs);
            foreach (var mob in _mobs)
            {
                if (HasComp<StoriesHereticComponent>(mob) || !_mobState.IsAlive(mob))
                    continue;

                _stamina.TakeStaminaDamage(mob, mask.StaminaDamage, source: wearer);
                _statusEffects.TryUpdateStatusEffectDuration(mob, HallucinationEffect, mask.HallucinationTime);
            }
        }
    }

    private void OnMaskEquipped(Entity<StoriesHereticMadnessMaskComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.MASK) == 0)
            return;

        ent.Comp.Wearer = args.EquipTarget;
        ent.Comp.NextPulse = _timing.CurTime + ent.Comp.Interval;
    }

    private void OnMaskUnequipped(Entity<StoriesHereticMadnessMaskComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;
    }

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        if (args.Effect != StoriesHereticRitualEffect.Ascension
            || args.Heretic.Comp.Path != AshPath
            || !ProtoMan.TryIndex(args.Ritual, out var proto)
            || !proto.TryComp<StoriesHereticAshAscensionRitualComponent>(out var ritual, _factory))
            return;

        if (args.Heretic.Comp.Ascended || !args.Heretic.Comp.CanAscend)
        {
            args.FailReason = Loc.GetString(args.Heretic.Comp.Ascended
                ? "stories-heretic-ascension-done"
                : "stories-heretic-learn-ascension-locked");
            return;
        }

        var corpses = new List<EntityUid>();
        foreach (var uid in args.Nearby)
        {
            if (corpses.Count >= ritual.Corpses)
                break;

            if (HasComp<HumanoidProfileComponent>(uid) && _mobState.IsDead(uid) && IsBurnt(uid, ritual.HeatThreshold))
                corpses.Add(uid);
        }

        if (corpses.Count < ritual.Corpses)
        {
            if (corpses.Count > 0)
                args.FailReason = Loc.GetString("stories-heretic-ash-ascension-corpses", ("count", ritual.Corpses - corpses.Count));
            return;
        }

        foreach (var corpse in corpses)
        {
            DropEverything(corpse);
        }

        args.Consumed.AddRange(corpses);
        Ascend(args.Heretic, ritual);
        _heretic.RemoveRitual(args.Heretic, args.Ritual);
        args.Success = true;
    }

    private void DropEverything(EntityUid uid)
    {
        if (_inventory.TryGetContainerSlotEnumerator(uid, out var slots))
        {
            while (slots.NextItem(out _, out var slot))
            {
                _inventory.TryUnequip(uid, slot.Name, true, true);
            }
        }

        _hands.DropAll(uid, checkActionBlocker: false, doDropInteraction: false);
    }

    private bool IsBurnt(EntityUid uid, FixedPoint2 threshold)
    {
        if (IsBurning(uid))
            return true;

        return TryComp<DamageableComponent>(uid, out var damageable)
               && _damageable.GetPositiveDamage((uid, damageable)).DamageDict.TryGetValue("Heat", out var heat)
               && heat >= threshold;
    }

    private void Ascend(Entity<StoriesHereticComponent> heretic, StoriesHereticAshAscensionRitualComponent ritual)
    {
        _heretic.SetAscended(heretic);

        foreach (var action in ritual.Actions)
        {
            _actions.AddAction(heretic, action);
        }

        _damageable.SetDamageModifierSetId(heretic.Owner, ritual.DamageModifierSet);
        _statusEffects.TrySetStatusEffectDuration(heretic, PressureImmunityEffect);

        if (ritual.Aura is { } aura)
            SpawnAttachedTo(aura, new EntityCoordinates(heretic, Vector2.Zero));

        _chat.DispatchGlobalAnnouncement(Loc.GetString(ritual.Announcement, ("name", Name(heretic))),
            colorOverride: ritual.AnnouncementColor);
        _audio.PlayGlobal(ritual.Sound, Filter.Broadcast(), true);
    }

    /// <summary>
    /// Языки пламени по окружности кольца. Только визуал: поджигает сам импульс кольца.
    /// </summary>
    private void SpawnRingEffects(Entity<StoriesHereticFireRingComponent> ring)
    {
        var center = _transform.GetMapCoordinates(ring);
        for (var i = 0; i < ring.Comp.EffectCount; i++)
        {
            var angle = new Angle(MathF.Tau * i / ring.Comp.EffectCount);
            Spawn(ring.Comp.Effect, center.Offset(angle.ToWorldVec() * ring.Comp.Radius));
        }
    }

    private void OnFireRing(Entity<StoriesHereticComponent> ent, ref StoriesHereticFireRingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var ring = EnsureComp<StoriesHereticFireRingComponent>(ent);
        ring.EndTime = _timing.CurTime + args.Duration;
        ring.NextPulse = _timing.CurTime;
    }

    private void OnFireWave(Entity<StoriesHereticComponent> ent, ref StoriesHereticFireWaveEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var origin = _transform.GetMapCoordinates(ent).Position;
        var facing = _transform.GetWorldRotation(ent).ToWorldVec();
        var minDot = MathF.Cos((float) Angle.FromDegrees(args.HalfAngleDegrees).Theta);

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, args.Range, _mobs);
        foreach (var mob in _mobs)
        {
            if (mob.Owner == ent.Owner)
                continue;

            var direction = _transform.GetMapCoordinates(mob).Position - origin;
            if (direction.LengthSquared() < 0.01f || Vector2.Dot(Vector2.Normalize(direction), facing) < minDot)
                continue;

            _damageable.TryChangeDamage(mob.Owner, args.Damage, origin: ent);
            Ignite(mob, args.FireStacks);
        }

        SpawnWaveEffects(_transform.GetMapCoordinates(ent), facing, args);
    }

    /// <summary>
    /// Языки пламени по конусу волны: на каждом шаге дальности несколько точек поперёк конуса.
    /// </summary>
    private void SpawnWaveEffects(MapCoordinates origin, Vector2 facing, StoriesHereticFireWaveEvent args)
    {
        var halfAngle = Angle.FromDegrees(args.HalfAngleDegrees);
        for (var distance = 1; distance <= (int) args.Range; distance++)
        {
            for (var i = 0; i < args.EffectsPerStep; i++)
            {
                var t = args.EffectsPerStep == 1 ? 0.5f : (float) i / (args.EffectsPerStep - 1);
                var angle = new Angle(halfAngle.Theta * (2 * t - 1));
                Spawn(args.Effect, origin.Offset(angle.RotateVec(facing) * distance));
            }
        }
    }
}
