using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticInfluenceSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly EntProtoId HallucinationEffect = "StatusEffectSeeingRainbow";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticInfluenceComponent, InteractHandEvent>(OnInfluenceInteract);
        SubscribeLocalEvent<StoriesHereticInfluenceComponent, StoriesHereticHarvestDoAfterEvent>(OnHarvest);
        SubscribeLocalEvent<StoriesHereticPiercedRealityComponent, ExaminedEvent>(OnPiercedExamined);
        SubscribeLocalEvent<StoriesHereticPiercedRealityComponent, InteractHandEvent>(OnPiercedInteract);
    }

    private void OnInfluenceInteract(Entity<StoriesHereticInfluenceComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || !HasComp<StoriesHereticComponent>(args.User))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.HarvestTime,
            new StoriesHereticHarvestDoAfterEvent(),
            ent,
            target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DuplicateCondition = DuplicateConditions.SameTarget,
        });
    }

    private void OnHarvest(Entity<StoriesHereticInfluenceComponent> ent, ref StoriesHereticHarvestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (_net.IsClient || !TryComp<StoriesHereticComponent>(args.User, out var heretic))
            return;

        if (TerminatingOrDeleted(ent) || EntityManager.IsQueuedForDeletion(ent))
            return;

        var coords = Transform(ent).Coordinates;
        var points = ent.Comp.Points * _heretic.GetInfluenceMultiplier(args.User);
        _heretic.AddPoints((args.User, heretic), points);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):user} harvested heretic influence {ToPrettyString(ent):target} for {points} points");
        _audio.PlayPvs(ent.Comp.HarvestSound, coords);
        Spawn(ent.Comp.PiercedPrototype, coords);
        QueueDel(ent);
    }

    private void OnPiercedExamined(Entity<StoriesHereticPiercedRealityComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<StoriesHereticComponent>(args.Examiner))
        {
            args.PushMarkup(Loc.GetString("stories-heretic-pierced-examine-heretic"));
            return;
        }

        args.PushMarkup(Loc.GetString("stories-heretic-pierced-examine"));
        if (_net.IsClient)
            return;

        _stamina.TakeStaminaDamage(args.Examiner, ent.Comp.ExamineStaminaDamage, source: ent);
        _statusEffects.TryUpdateStatusEffectDuration(args.Examiner, HallucinationEffect, ent.Comp.ExamineHallucinationTime);
    }

    private void OnPiercedInteract(Entity<StoriesHereticPiercedRealityComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || HasComp<StoriesHereticComponent>(args.User))
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString("stories-heretic-pierced-touch"), args.User, args.User, PopupType.MediumCaution);
        if (_random.Prob(ent.Comp.TouchChance))
            _damageable.TryChangeDamage(args.User, ent.Comp.TouchDamage, true);
    }
}
