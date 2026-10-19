using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Popups;
using Content.Server.Shuttles.Systems;
using Content.Shared.Popups;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Administration;
using Content.Shared.Antag;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticAdminVerbSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private ArrivalsSystem _arrivals = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedRoleSystem _roles = default!;

    private static readonly EntProtoId HereticRule = "STHeretic";
    private static readonly ProtoId<AntagSpecifierPrototype> HereticSpecifier = "STHereticSpecifier";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor)
            || !_admin.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        if (!TryComp<MindContainerComponent>(args.Target, out var container)
            || !TryComp<ActorComponent>(args.Target, out var targetActor)
            || HasComp<StoriesHereticComponent>(args.Target)
            || container.Mind is { } mindId && _roles.MindHasRole<StoriesHereticRoleComponent>(mindId))
            return;

        var target = targetActor.PlayerSession;
        var targetUid = args.Target;
        var admin = args.User;
        var name = Loc.GetString("stories-heretic-admin-verb-make");
        var onArrivals = _arrivals.IsOnArrivals((targetUid, null));
        args.Verbs.Add(new Verb
        {
            Text = name,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_Stories/Heretic/actions.rsi"), "eye"),
            Act = () =>
            {
                _antag.ForceMakeAntag<StoriesHereticRuleComponent>(target, HereticRule, HereticSpecifier);
                if (!HasComp<StoriesHereticComponent>(targetUid))
                    _popup.PopupEntity(Loc.GetString("stories-heretic-admin-verb-failed"), admin, admin, PopupType.MediumCaution);
            },
            Impact = LogImpact.High,
            Disabled = onArrivals,
            Message = onArrivals
                ? Loc.GetString("stories-heretic-admin-verb-arrivals")
                : string.Join(": ", name, Loc.GetString("stories-heretic-admin-verb-make-message")),
        });
    }
}
