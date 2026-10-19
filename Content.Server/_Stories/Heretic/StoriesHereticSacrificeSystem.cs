using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Administration.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cuffs;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticSacrificeSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StoriesHereticObjectiveSystem _objectives = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private StoriesHereticRuleSystem _rule = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<DepartmentPrototype> SecurityDepartment = "Security";
    private static readonly EntProtoId HallucinationEffect = "StatusEffectSeeingRainbow";
    private static readonly EntProtoId MessPrototype = "PuddleBlood";

    private readonly List<(EntityUid Uid, ProtoId<JobPrototype>? Job, bool Head)> _candidates = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticLivingHeartEvent>(OnLivingHeart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticShadowRealmComponent>();
        while (query.MoveNext(out var uid, out var realm))
        {
            if (now >= realm.ReturnTime)
            {
                ReturnFromRealm((uid, realm));
                continue;
            }

            if (now < realm.NextDamage)
                continue;

            realm.NextDamage = now + realm.DamageInterval;
            _damageable.TryChangeDamage(uid, realm.Damage, true);
            _statusEffects.TryUpdateStatusEffectDuration(uid, HallucinationEffect, realm.HallucinationTime);
        }
    }

    #region Targets

    public void AssignTargets(Entity<StoriesHereticComponent> heretic)
    {
        _candidates.Clear();
        var query = EntityQueryEnumerator<HumanoidProfileComponent, MobStateComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState, out _))
        {
            if (uid == heretic.Owner
                || HasComp<StoriesHereticComponent>(uid)
                || HasComp<StoriesHereticSacrificedComponent>(uid)
                || HasComp<StoriesHereticShadowRealmComponent>(uid)
                || !_rule.IsOnStation(uid))
                continue;

            var head = IsHead(uid);
            ProtoId<JobPrototype>? job = null;
            if (_mind.TryGetMind(uid, out var mindId, out _))
                _jobs.MindTryGetJobId(mindId, out job);
            else if (!head)
                continue;

            if (!head && !_mobState.IsAlive(uid, mobState))
                continue;

            _candidates.Add((uid, job, head));
        }

        _random.Shuffle(_candidates);

        var picked = new List<EntityUid>();
        var headPicked = TryPick(picked, c => c.Head);
        TryPick(picked, c => InDepartment(c.Job, SecurityDepartment));
        if (GetPrimaryDepartment(heretic) is { } own)
            TryPick(picked, c => InDepartment(c.Job, own));

        while (picked.Count < heretic.Comp.TargetCount && TryPick(picked, _ => true))
        {
        }

        _heretic.SetTargets(heretic, picked);

        if (!headPicked && heretic.Comp.HeadSacrificeCount < heretic.Comp.RequiredHeadSacrifices)
            WaiveHeadSacrifice(heretic);
    }

    private void WaiveHeadSacrifice(Entity<StoriesHereticComponent> heretic)
    {
        _heretic.SetRequiredHeadSacrifices(heretic, 0);

        if (_mind.TryGetMind(heretic, out var mindId, out var mind))
            _objectives.RefreshTitles(mindId, mind);

        if (TryComp<ActorComponent>(heretic, out var actor))
            _chat.DispatchServerMessage(actor.PlayerSession, Loc.GetString("stories-heretic-head-sacrifice-waived"));
    }

    private bool TryPick(List<EntityUid> picked, Func<(EntityUid Uid, ProtoId<JobPrototype>? Job, bool Head), bool> predicate)
    {
        for (var i = 0; i < _candidates.Count; i++)
        {
            if (!predicate(_candidates[i]))
                continue;

            picked.Add(_candidates[i].Uid);
            _candidates.RemoveAt(i);
            return true;
        }

        return false;
    }

    private bool InDepartment(ProtoId<JobPrototype>? job, ProtoId<DepartmentPrototype> department)
    {
        return job is { } jobId && ProtoMan.Index(department).Roles.Contains(jobId);
    }

    private ProtoId<DepartmentPrototype>? GetPrimaryDepartment(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !_jobs.MindTryGetJobId(mindId, out var job)
            || job is not { } jobId
            || !_jobs.TryGetPrimaryDepartment(jobId.Id, out var department))
            return null;

        return department.ID;
    }

    public bool IsHead(EntityUid uid)
    {
        return HasComp<CommandStaffComponent>(uid);
    }

    #endregion

    #region Rituals

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        switch (args.Effect)
        {
            case StoriesHereticRitualEffect.Sacrifice:
                TrySacrifice(ref args);
                break;
            case StoriesHereticRitualEffect.RerollTargets:
                AssignTargets(args.Heretic);
                args.Success = true;
                break;
        }
    }

    private void TrySacrifice(ref StoriesHereticRitualEffectEvent args)
    {
        var heretic = args.Heretic;
        foreach (var uid in args.Nearby)
        {
            if (!HasComp<HumanoidProfileComponent>(uid) || !HasComp<MobStateComponent>(uid))
                continue;

            if (!_heretic.IsTarget(heretic, uid))
            {
                args.FailReason ??= Loc.GetString("stories-heretic-sacrifice-not-target");
                continue;
            }

            if (HasComp<StoriesHereticSacrificedComponent>(uid))
            {
                args.FailReason ??= Loc.GetString("stories-heretic-sacrifice-already");
                continue;
            }

            if (!CanBeSacrificed(uid))
            {
                args.FailReason ??= Loc.GetString("stories-heretic-sacrifice-resisting");
                continue;
            }

            Sacrifice(heretic, uid, Transform(args.Rune).Coordinates);
            args.Success = true;
            return;
        }
    }

    private bool CanBeSacrificed(EntityUid uid)
    {
        return _mobState.IsIncapacitated(uid)
               || _cuffable.IsCuffed(uid)
               || HasComp<SleepingComponent>(uid);
    }

    private void Sacrifice(Entity<StoriesHereticComponent> heretic, EntityUid victim, EntityCoordinates runeCoords)
    {
        _heretic.RegisterSacrifice(heretic, victim, IsHead(victim));

        if (!_mind.TryGetMind(victim, out _, out _))
            Spawn(MessPrototype, runeCoords);

        _rejuvenate.PerformRejuvenate(victim);
        EnsureComp<StoriesHereticSacrificedComponent>(victim);
        SendToShadowRealm(victim, runeCoords);
        _popup.PopupEntity(Loc.GetString("stories-heretic-sacrifice-victim"), victim, victim, PopupType.LargeCaution);
    }

    #endregion

    #region Living heart

    private void OnLivingHeart(Entity<StoriesHereticComponent> ent, ref StoriesHereticLivingHeartEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _audio.PlayPvs(args.Sound, ent);

        if (_heretic.CycleTrackedTarget(ent) is not { } target)
        {
            _popup.PopupEntity(Loc.GetString("stories-heretic-heart-no-targets"), ent, ent);
            return;
        }

        var from = _transform.GetMapCoordinates(ent);
        var to = _transform.GetMapCoordinates(target);

        string text;
        if (from.MapId != to.MapId)
        {
            text = Loc.GetString("stories-heretic-heart-other-map", ("target", Name(target)));
        }
        else
        {
            var delta = to.Position - from.Position;
            var distance = delta.Length();
            var range = distance < args.NearDistance ? "near" : distance < args.MediumDistance ? "medium" : "far";
            text = Loc.GetString("stories-heretic-heart-direction",
                ("target", Name(target)),
                ("direction", Loc.GetString($"stories-heretic-direction-{delta.ToWorldAngle().GetDir()}")),
                ("range", Loc.GetString($"stories-heretic-range-{range}")));
        }

        _popup.PopupEntity(text, ent, ent, PopupType.Medium);
    }

    #endregion
}
