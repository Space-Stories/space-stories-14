using System.Diagnostics.CodeAnalysis;
using Content.Server._Stories.Conversion;
using Content.Server.Chat.Managers;
using Content.Shared._Stories.Conversion;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ghouls;

public sealed partial class StoriesHereticGhoulSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ConversionSystem _conversion = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticGhoulComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoriesHereticGhoulComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoriesHereticGhoulComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<StoriesHereticGhoulComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StoriesHereticRaisedComponent, MobStateChangedEvent>(OnRaisedMobStateChanged);
    }

    public int CountGhouls(EntityUid master, ProtoId<ConversionPrototype> conversion)
    {
        return _conversion.GetEntitiesConvertedBy(master, conversion).Count;
    }

    public bool TryRaise(EntityUid master,
        EntityUid corpse,
        ProtoId<ConversionPrototype> conversion,
        int limit,
        [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        var mindId = EntityUid.Invalid;
        MindComponent? mind = null;
        if (!HasComp<HumanoidProfileComponent>(corpse) || !_mobState.IsDead(corpse))
            reason = Loc.GetString("stories-heretic-ghoul-invalid");
        else if (HasComp<StoriesHereticRaisedComponent>(corpse))
            reason = Loc.GetString("stories-heretic-ghoul-already");
        // Душа — разум игрока, даже если он сейчас призраком: без игрока гуль был бы пустой оболочкой.
        else if (!_mind.TryGetMind(corpse, out mindId, out mind) || mind.UserId == null)
            reason = Loc.GetString("stories-heretic-ghoul-no-soul");
        else if (CountGhouls(master, conversion) >= limit)
            reason = Loc.GetString("stories-heretic-ghoul-limit");
        else if (!_conversion.CanConvert(corpse, conversion, master))
            reason = Loc.GetString("stories-heretic-ghoul-protected");

        if (reason != null)
            return false;

        // Вышедший из тела игрок лишь навещает призрака — возвращаем его в тело.
        _mind.UnVisit(mindId, mind!);
        _rejuvenate.PerformRejuvenate(corpse);
        EnsureComp<StoriesHereticRaisedComponent>(corpse);
        _conversion.TryConvert(corpse, conversion, master);
        _heretic.MakeServant(corpse, master);

        if (_player.TryGetSessionById(mind!.UserId, out var session))
            _chat.DispatchServerMessage(session, Loc.GetString("stories-heretic-ghoul-briefing", ("master", master)));

        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(master):user} raised {ToPrettyString(corpse):target} as a ghoul");
        return true;
    }

    private void OnStartup(Entity<StoriesHereticGhoulComponent> ent, ref ComponentStartup args)
    {
        var raised = EnsureComp<StoriesHereticRaisedComponent>(ent);
        if (_thresholds.TryGetThresholdForState(ent, MobState.Critical, out var crit))
            raised.OriginalCrit ??= crit;
        if (_thresholds.TryGetThresholdForState(ent, MobState.Dead, out var dead))
            raised.OriginalDead ??= dead;

        _thresholds.SetMobStateThreshold(ent, ent.Comp.CritThreshold, MobState.Critical);
        _thresholds.SetMobStateThreshold(ent, ent.Comp.DeadThreshold, MobState.Dead);
    }

    /// <summary>
    /// Живому (снятие майндшилдом или админом) пороги возвращаются сразу. Мёртвый гуль остаётся трупом:
    /// с обычными порогами накопленный урон снова оказался бы «живым». Ему пороги вернёт оживление.
    /// </summary>
    private void OnShutdown(Entity<StoriesHereticGhoulComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemComp<StoriesHereticServantComponent>(ent);
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent):target} is no longer a ghoul");

        if (!_mobState.IsDead(ent) && TryComp<StoriesHereticRaisedComponent>(ent, out var raised))
            RestoreThresholds((ent, raised));
    }

    private void OnRaisedMobStateChanged(Entity<StoriesHereticRaisedComponent> ent, ref MobStateChangedEvent args)
    {
        // Бывший гуль оживлён (дефибриллятор, реанимация).
        if (args.OldMobState == MobState.Dead && args.NewMobState != MobState.Dead && !HasComp<StoriesHereticGhoulComponent>(ent))
            RestoreThresholds(ent);
    }

    private void RestoreThresholds(Entity<StoriesHereticRaisedComponent> ent)
    {
        // Сначала очищаем: смена порогов сама вызывает смену состояния и повторный вход сюда.
        var dead = ent.Comp.OriginalDead;
        var crit = ent.Comp.OriginalCrit;
        ent.Comp.OriginalDead = null;
        ent.Comp.OriginalCrit = null;

        if (dead != null)
            _thresholds.SetMobStateThreshold(ent, dead.Value, MobState.Dead);
        if (crit != null)
            _thresholds.SetMobStateThreshold(ent, crit.Value, MobState.Critical);
    }

    private void OnMobStateChanged(Entity<StoriesHereticGhoulComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !TryComp<ConversionableComponent>(ent, out var conversions))
            return;

        // Гуль умер — служба окончена, слот лимита освобождается.
        foreach (var id in new List<string>(conversions.ActiveConversions.Keys))
        {
            if (ProtoMan.TryIndex<ConversionPrototype>(id, out var proto) && proto.Components.ContainsKey(Factory.GetComponentName<StoriesHereticGhoulComponent>()))
                _conversion.TryRevert(ent, id);
        }
    }

    private void OnExamined(Entity<StoriesHereticGhoulComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("stories-heretic-ghoul-examine", ("ent", ent.Owner)));
    }
}
