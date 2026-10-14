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
using Content.Shared.StatusEffectNew;
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
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly EntProtoId ServeMaster = "STHereticObjectiveServeMaster";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticGhoulComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoriesHereticGhoulComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoriesHereticGhoulComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<StoriesHereticGhoulComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StoriesHereticRaisedComponent, MobStateChangedEvent>(OnRaisedMobStateChanged);
        InitializeRitual();
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
        reason = GetRaiseFailure(master, corpse, conversion, limit, true);
        if (reason != null)
            return false;

        _mind.TryGetMind(corpse, out var mindId, out var mind);

        // Вышедший из тела игрок лишь навещает призрака — возвращаем его в тело.
        _mind.UnVisit(mindId, mind!);
        _rejuvenate.PerformRejuvenate(corpse);
        EnsureComp<StoriesHereticRaisedComponent>(corpse);
        _conversion.TryConvert(corpse, conversion, master);
        _heretic.MakeServant(corpse, master);
        _mind.TryAddObjective(mindId, mind!, ServeMaster);

        if (_player.TryGetSessionById(mind!.UserId, out var session))
            _chat.DispatchServerMessage(session, Loc.GetString("stories-heretic-ghoul-briefing", ("master", master)));

        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(master):user} raised {ToPrettyString(corpse):target} as a ghoul");
        return true;
    }

    /// <summary>
    /// Порядок проверок по спецификации §6.2: лимит, гуманоид, мёртв, не поднимался, душа, защита.
    /// <paramref name="needSoul"/> выключен для трупа, на который лишь ставится призрачная роль.
    /// </summary>
    private string? GetRaiseFailure(EntityUid master, EntityUid corpse, ProtoId<ConversionPrototype> conversion, int limit, bool needSoul)
    {
        // Ожидающие игрока предложения тоже занимают слот: иначе их можно наплодить сверх лимита.
        if (CountGhouls(master, conversion) + CountOffers(master, conversion) >= limit)
            return Loc.GetString("stories-heretic-ghoul-limit");
        if (!HasComp<HumanoidProfileComponent>(corpse) || !_mobState.IsDead(corpse))
            return Loc.GetString("stories-heretic-ghoul-invalid");
        if (HasComp<StoriesHereticRaisedComponent>(corpse))
            return Loc.GetString("stories-heretic-ghoul-already");
        // Душа — разум игрока, даже если он сейчас призраком: без игрока гуль был бы пустой оболочкой.
        if (needSoul && !HasSoul(corpse))
            return Loc.GetString("stories-heretic-ghoul-no-soul");
        if (!_conversion.CanConvert(corpse, conversion, master))
            return Loc.GetString("stories-heretic-ghoul-protected");
        return null;
    }

    private int CountOffers(EntityUid master, ProtoId<ConversionPrototype> conversion)
    {
        var count = 0;
        var query = EntityQueryEnumerator<StoriesHereticGhoulOfferComponent>();
        while (query.MoveNext(out _, out var offer))
        {
            if (offer.Master == master && offer.Conversion == conversion)
                count++;
        }

        return count;
    }

    public bool HasSoul(EntityUid corpse)
    {
        return _mind.TryGetMind(corpse, out _, out var mind) && mind.UserId != null;
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

        if (ent.Comp.StatusEffect is { } effect)
            _statusEffects.TrySetStatusEffectDuration(ent, effect);
    }

    /// <summary>
    /// Живому (снятие майндшилдом или админом) пороги возвращаются сразу. Мёртвый гуль остаётся трупом:
    /// с обычными порогами накопленный урон снова оказался бы «живым». Ему пороги вернёт оживление.
    /// </summary>
    private void OnShutdown(Entity<StoriesHereticGhoulComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (ent.Comp.StatusEffect is { } effect)
            _statusEffects.TryRemoveStatusEffect(ent, effect);

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
