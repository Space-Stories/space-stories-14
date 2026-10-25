using Content.Server.Body.Components;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidSystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private SharedPointLightSystem _light = default!;

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
            if (!HasComp<StoriesHereticComponent>(uid))
            {
                RemCompDeferred<StoriesHereticVoidStormComponent>(uid);
                continue;
            }

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

        if (HasComp<PointLightComponent>(ent))
            return;

        var light = AddComp<PointLightComponent>(ent);
        ent.Comp.LightAdded = true;
        _light.SetRadius(ent, ent.Comp.LightRadius, light);
        _light.SetEnergy(ent, ent.Comp.LightEnergy, light);
        _light.SetColor(ent, ent.Comp.LightColor, light);
    }

    private void OnStormShutdown(Entity<StoriesHereticVoidStormComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.LightAdded)
            RemComp<PointLightComponent>(ent);

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
            args.FailScore = 2;
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
            args.FailScore = 2;
            if (corpses.Count > 0)
                args.FailReason = Loc.GetString("stories-heretic-void-ascension-corpses", ("count", ritual.Corpses - corpses.Count));
            else if (HasLivingHumanoid(args.Nearby))
                args.FailReason = Loc.GetString("stories-heretic-ascension-not-dead");
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

    private bool HasLivingHumanoid(HashSet<EntityUid> nearby)
    {
        foreach (var uid in nearby)
        {
            if (HasComp<HumanoidProfileComponent>(uid) && !_mobState.IsDead(uid))
                return true;
        }

        return false;
    }

    private void Ascend(Entity<StoriesHereticComponent> heretic, StoriesHereticVoidAscensionRitualComponent ritual)
    {
        _heretic.SetAscended(heretic);

        _statusEffects.TrySetStatusEffectDuration(heretic, PressureImmunityEffect);
        EnsureComp<StoriesHereticColdImmuneComponent>(heretic);
        EntityManager.AddComponents(heretic, ritual.AddComponents, removeExisting: true);
        if (ritual.RemoveBreathing)
            RemComp<RespiratorComponent>(heretic);

        _chat.DispatchGlobalAnnouncement(Loc.GetString(ritual.Announcement, ("name", Name(heretic))),
            colorOverride: ritual.AnnouncementColor);
        _audio.PlayGlobal(ritual.Sound, Filter.Broadcast(), true);

        var music = AudioParams.Default.WithLoop(true).WithVolume(SharedAudioSystem.GainToVolume(ritual.MusicGain));
        if (TryComp<StoriesHereticVoidStormComponent>(heretic, out var storm))
            storm.MusicStream = _audio.PlayPvs(ritual.Music, heretic, music)?.Entity;
    }
}
