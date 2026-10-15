using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Raffles;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Shared.Mobs;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Ghouls;

// Несовершенный ритуал: труп на руне поднимается сразу, а бездушный предлагается призракам.
public sealed partial class StoriesHereticGhoulSystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<GhostRoleRaffleSettingsPrototype> OfferRaffle = "short";
    private static readonly EntProtoId GhoulMindRole = "STMindRoleHereticGhoul";

    private void InitializeRitual()
    {
        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticGhoulOfferComponent, MindAddedMessage>(OnOfferMindAdded);
        SubscribeLocalEvent<StoriesHereticGhoulOfferComponent, MobStateChangedEvent>(OnOfferMobStateChanged);
        SubscribeLocalEvent<StoriesHereticGhoulOfferComponent, EntityTerminatingEvent>(OnOfferTerminating);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticGhoulOfferComponent>();
        while (query.MoveNext(out var uid, out var offer))
        {
            if (now < offer.ExpireTime)
                continue;

            // Никто не откликнулся: труп остаётся трупом, мак возвращается.
            _popup.PopupCoordinates(Loc.GetString("stories-heretic-flesh-imperfect-expired"), Transform(uid).Coordinates, PopupType.MediumCaution);
            EndOffer(uid, true);
        }
    }

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        if (args.Effect != StoriesHereticRitualEffect.Ghoul
            || !ProtoMan.TryIndex(args.Ritual, out var proto)
            || !proto.TryComp<StoriesHereticGhoulRitualComponent>(out var ritual, _factory))
            return;

        var limit = ritual.Limit + (args.Heretic.Comp.Ascended ? ritual.AscendedBonus : 0);
        EntityUid? soulless = null;
        var busy = false;
        foreach (var uid in args.Nearby)
        {
            if (HasComp<HumanoidProfileComponent>(uid) && !HasComp<StoriesHereticRaisedComponent>(uid) && _mobState.IsDead(uid))
            {
                if (HasSoul(uid))
                {
                    if (TryRaise(args.Heretic, uid, ritual.Conversion, limit, out var reason))
                    {
                        args.Success = true;
                        return;
                    }

                    args.FailReason = reason;
                    continue;
                }

                if (HasComp<StoriesHereticGhoulOfferComponent>(uid))
                    busy = true;
                else
                    soulless ??= uid;
            }
        }

        if (soulless is not { } corpse)
        {
            args.FailReason ??= Loc.GetString(busy ? "stories-heretic-flesh-imperfect-busy" : "stories-heretic-flesh-imperfect-no-body");
            return;
        }

        var failure = GetRaiseFailure(args.Heretic, corpse, ritual.Conversion, limit, false);
        if (failure != null)
        {
            args.FailReason = failure;
            return;
        }

        var offer = EnsureComp<StoriesHereticGhoulOfferComponent>(corpse);
        offer.Master = args.Heretic;
        offer.Conversion = ritual.Conversion;
        offer.Limit = limit;
        offer.Refund = ritual.Refund;
        offer.ExpireTime = _timing.CurTime + ritual.OfferTime;

        EnsureComp<GhostTakeoverAvailableComponent>(corpse);
        // Роль собирается до добавления: открытые окна призрачных ролей сразу видят её имя.
        // MindRoles нужен для проверки бана на роль гуля; саму роль снимает OnOfferMindAdded.
        var role = new GhostRoleComponent
        {
            RoleName = Loc.GetString("stories-heretic-flesh-voiceless-name"),
            RoleDescription = Loc.GetString("stories-heretic-flesh-voiceless-description"),
            RoleRules = Loc.GetString("stories-heretic-flesh-voiceless-rules"),
            MindRoles = [GhoulMindRole],
            RaffleConfig = new GhostRoleRaffleConfig(ProtoMan.Index(OfferRaffle).Settings),
        };
        AddComp(corpse, role, true);

        _popup.PopupEntity(Loc.GetString("stories-heretic-flesh-imperfect-offered"), corpse, args.Heretic);
        args.Success = true;
    }

    private void OnOfferMindAdded(Entity<StoriesHereticGhoulOfferComponent> ent, ref MindAddedMessage args)
    {
        var offer = ent.Comp;
        var (master, conversion, limit) = (offer.Master, offer.Conversion, offer.Limit);
        // Закрываем до подсчёта: само предложение не должно занимать слот лимита.
        var coords = Transform(ent).Coordinates;
        var refund = offer.Refund;
        // GhostRoleSystem выдаёт MindRoles уже после переноса разума, тем же объектом компонента.
        // Роль гуля выдаёт обращение, а при неудаче игрок остаётся простым трупом: выдавать нечего.
        if (TryComp<GhostRoleComponent>(ent, out var ghostRole))
            ghostRole.MindRoles.Clear();

        EndOffer(ent, false);
        string? reason;
        if (TerminatingOrDeleted(master))
            reason = Loc.GetString("stories-heretic-flesh-imperfect-no-master");
        else if (TryRaise(master, ent, conversion, limit, out reason))
            return;

        // Лимит заняли за 5 минут или труп испортился: игрок остаётся в трупе, мак возвращается.
        Spawn(refund, coords);
        if (args.Mind.Comp.UserId is { } user && _player.TryGetSessionById(user, out var session))
            _chat.DispatchServerMessage(session, reason);
    }

    private void OnOfferMobStateChanged(Entity<StoriesHereticGhoulOfferComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState == MobState.Dead && args.NewMobState != MobState.Dead)
            EndOffer(ent, true);
    }

    private void OnOfferTerminating(Entity<StoriesHereticGhoulOfferComponent> ent, ref EntityTerminatingEvent args)
    {
        // Штатно закрытое предложение уже разобрано. Иначе труп удалён или разорван: мак ляжет на его последнее место.
        if (ent.Comp.Settled || Transform(ent).MapUid is not { } map || TerminatingOrDeleted(map))
            return;

        Spawn(ent.Comp.Refund, Transform(ent).Coordinates);
    }

    private void EndOffer(EntityUid corpse, bool refund)
    {
        if (!TryComp<StoriesHereticGhoulOfferComponent>(corpse, out var offer))
            return;

        offer.Settled = true;
        if (refund)
            Spawn(offer.Refund, Transform(corpse).Coordinates);

        RemComp<StoriesHereticGhoulOfferComponent>(corpse);
        RemComp<GhostRoleComponent>(corpse);
        RemComp<GhostTakeoverAvailableComponent>(corpse);
    }
}
