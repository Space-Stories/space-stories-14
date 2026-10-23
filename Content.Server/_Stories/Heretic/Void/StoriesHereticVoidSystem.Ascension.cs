using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidSystem
{
    [Dependency] private ChatSystem _announcer = default!;
    [Dependency] private IComponentFactory _factory = default!;

    private static readonly EntProtoId PressureImmunityEffect = "StatusEffectPressureImmunity";

    private void InitializeAscension()
    {
        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticVoidStormComponent, ComponentStartup>(OnStormStartup);
        SubscribeLocalEvent<StoriesHereticVoidStormComponent, ComponentShutdown>(OnStormShutdown);
        SubscribeLocalEvent<StoriesHereticVoidStormComponent, MobStateChangedEvent>(OnStormMobState);
    }

    private void UpdateStorms(TimeSpan now)
    {
        var query = EntityQueryEnumerator<StoriesHereticVoidStormComponent>();
        while (query.MoveNext(out var uid, out var storm))
        {
            if (now < storm.NextPulse)
                continue;

            storm.NextPulse = now + storm.Interval;
            SpawnSnowflakes((uid, storm));

            _mobs.Clear();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, storm.Radius, _mobs);
            foreach (var mob in _mobs)
            {
                if (mob.Owner == uid
                    || HasComp<StoriesHereticComponent>(mob)
                    || _heretic.IsServant(mob)
                    || !_mobState.IsAlive(mob))
                    continue;

                _statusEffects.TryUpdateStatusEffectDuration(mob, MutedEffect, storm.MuteTime);

                if (_chill.IsImmune(mob))
                    continue;

                _chill.ApplyChill(mob, storm.ChillStacks);
                _damageable.TryChangeDamage(mob.Owner, storm.Damage, origin: uid);
            }
        }
    }

    private void SpawnSnowflakes(Entity<StoriesHereticVoidStormComponent> storm)
    {
        var center = _transform.GetMapCoordinates(storm);
        for (var i = 0; i < storm.Comp.Snowflakes.Count; i++)
        {
            var distance = storm.Comp.Radius * MathF.Sqrt(_random.NextFloat());
            SpawnFrost(storm.Comp.Snowflakes, center.Offset(_random.NextAngle().ToWorldVec() * distance));
        }
    }

    private void OnStormStartup(Entity<StoriesHereticVoidStormComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextPulse = _timing.CurTime;
        ent.Comp.RingEntity = SpawnAttachedTo(ent.Comp.RingEffect, new EntityCoordinates(ent, Vector2.Zero));
        _scale.SetSpriteScale(ent.Comp.RingEntity.Value, new Vector2(ent.Comp.Radius / ent.Comp.RingSpriteRadius));
    }

    private void OnStormShutdown(Entity<StoriesHereticVoidStormComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.RingEntity);
        _audio.Stop(ent.Comp.MusicStream);
    }

    private void OnStormMobState(Entity<StoriesHereticVoidStormComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemCompDeferred<StoriesHereticVoidStormComponent>(ent);
    }

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        if (args.Effect != StoriesHereticRitualEffect.Ascension
            || args.Heretic.Comp.Path != VoidPath
            || !ProtoMan.TryIndex(args.Ritual, out var proto)
            || !proto.TryComp<StoriesHereticVoidAscensionRitualComponent>(out var ritual, _factory))
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

            if (HasComp<HumanoidProfileComponent>(uid) && _mobState.IsDead(uid))
                corpses.Add(uid);
        }

        if (corpses.Count < ritual.Corpses)
        {
            if (corpses.Count > 0)
                args.FailReason = Loc.GetString("stories-heretic-void-ascension-corpses", ("count", ritual.Corpses - corpses.Count));
            return;
        }

        foreach (var corpse in corpses)
        {
            _heretic.DropEverything(corpse);
        }

        args.Consumed.AddRange(corpses);
        Ascend(args.Heretic, ritual);
        _heretic.RemoveRitual(args.Heretic, args.Ritual);
        args.Success = true;
    }

    private void Ascend(Entity<StoriesHereticComponent> heretic, StoriesHereticVoidAscensionRitualComponent ritual)
    {
        _heretic.SetAscended(heretic);

        _statusEffects.TrySetStatusEffectDuration(heretic, PressureImmunityEffect);
        EnsureComp<StoriesHereticColdImmuneComponent>(heretic);
        EntityManager.AddComponents(heretic, ritual.AddComponents, removeExisting: true);
        if (ritual.RemoveBreathing)
            RemComp<RespiratorComponent>(heretic);

        _announcer.DispatchGlobalAnnouncement(Loc.GetString(ritual.Announcement, ("name", Name(heretic))),
            colorOverride: ritual.AnnouncementColor);
        _audio.PlayGlobal(ritual.Sound, Filter.Broadcast(), true);

        var music = AudioParams.Default.WithLoop(true).WithVolume(SharedAudioSystem.GainToVolume(ritual.MusicGain));
        if (TryComp<StoriesHereticVoidStormComponent>(heretic, out var storm))
            storm.MusicStream = _audio.PlayPvs(ritual.Music, heretic, music)?.Entity;
    }
}
