using System.Diagnostics.CodeAnalysis;
using Content.Server._Stories.Conversion;
using Content.Server.Chat.Managers;
using Content.Shared._Stories.Conversion;
using Content.Shared.Administration.Systems;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ghouls;

public sealed partial class StoriesHereticGhoulSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private ConversionSystem _conversion = default!;
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
        if (!HasComp<HumanoidProfileComponent>(corpse) || !_mobState.IsDead(corpse))
            reason = Loc.GetString("stories-heretic-ghoul-invalid");
        else if (HasComp<StoriesHereticRaisedComponent>(corpse))
            reason = Loc.GetString("stories-heretic-ghoul-already");
        else if (CountGhouls(master, conversion) >= limit)
            reason = Loc.GetString("stories-heretic-ghoul-limit");
        else if (!_conversion.CanConvert(corpse, conversion, master))
            reason = Loc.GetString("stories-heretic-ghoul-protected");

        if (reason != null)
            return false;

        _rejuvenate.PerformRejuvenate(corpse);
        EnsureComp<StoriesHereticRaisedComponent>(corpse);
        _conversion.TryConvert(corpse, conversion, master);

        if (TryComp<ActorComponent>(corpse, out var actor))
            _chat.DispatchServerMessage(actor.PlayerSession, Loc.GetString("stories-heretic-ghoul-briefing", ("master", master)));

        return true;
    }

    private void OnStartup(Entity<StoriesHereticGhoulComponent> ent, ref ComponentStartup args)
    {
        if (_thresholds.TryGetThresholdForState(ent, MobState.Critical, out var crit))
            ent.Comp.OriginalCrit = crit;
        if (_thresholds.TryGetThresholdForState(ent, MobState.Dead, out var dead))
            ent.Comp.OriginalDead = dead;

        _thresholds.SetMobStateThreshold(ent, ent.Comp.CritThreshold, MobState.Critical);
        _thresholds.SetMobStateThreshold(ent, ent.Comp.DeadThreshold, MobState.Dead);
    }

    /// <summary>
    /// Пороги возвращаются только живому (снятие майндшилдом или админом). Мёртвый гуль остаётся трупом:
    /// с обычными порогами накопленный урон снова оказался бы «живым».
    /// </summary>
    private void OnShutdown(Entity<StoriesHereticGhoulComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || _mobState.IsDead(ent))
            return;

        if (ent.Comp.OriginalDead is { } dead)
            _thresholds.SetMobStateThreshold(ent, dead, MobState.Dead);
        if (ent.Comp.OriginalCrit is { } crit)
            _thresholds.SetMobStateThreshold(ent, crit, MobState.Critical);
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
