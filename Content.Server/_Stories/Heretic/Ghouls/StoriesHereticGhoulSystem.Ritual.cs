using Content.Server.Ghost.Roles.Components;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Ghouls;

// Несовершенный ритуал: труп на руне поднимается сразу, а бездушный предлагается призракам.
public sealed partial class StoriesHereticGhoulSystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private void InitializeRitual()
    {
        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticGhoulOfferComponent, MindAddedMessage>(OnOfferMindAdded);
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
            var coords = Transform(uid).Coordinates;
            Spawn(offer.Refund, coords);
            _popup.PopupCoordinates(Loc.GetString("stories-heretic-flesh-imperfect-expired"), coords, PopupType.MediumCaution);
            EndOffer(uid);
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

                soulless ??= uid;
            }
        }

        if (soulless is not { } corpse)
        {
            args.FailReason ??= Loc.GetString("stories-heretic-flesh-imperfect-no-body");
            return;
        }

        var failure = GetRaiseFailure(args.Heretic, corpse, ritual.Conversion, limit, false);
        if (failure == null && HasComp<StoriesHereticGhoulOfferComponent>(corpse))
            failure = Loc.GetString("stories-heretic-flesh-imperfect-busy");
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
        var role = EnsureComp<GhostRoleComponent>(corpse);
        role.RoleName = Loc.GetString("stories-heretic-flesh-voiceless-name");
        role.RoleDescription = Loc.GetString("stories-heretic-flesh-voiceless-description");
        role.RoleRules = Loc.GetString("stories-heretic-flesh-voiceless-rules");
        role.MindRoles = new();

        _popup.PopupEntity(Loc.GetString("stories-heretic-flesh-imperfect-offered"), corpse, args.Heretic);
        args.Success = true;
    }

    private void OnOfferMindAdded(Entity<StoriesHereticGhoulOfferComponent> ent, ref MindAddedMessage args)
    {
        var offer = ent.Comp;
        // Лимит мог занять другой ритуал или Хватка за 5 минут — тогда игрок остаётся в трупе.
        TryRaise(offer.Master, ent, offer.Conversion, offer.Limit, out _);
        EndOffer(ent);
    }

    private void EndOffer(EntityUid corpse)
    {
        RemComp<StoriesHereticGhoulOfferComponent>(corpse);
        RemComp<GhostRoleComponent>(corpse);
        RemComp<GhostTakeoverAvailableComponent>(corpse);
    }
}
