using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Administration;
using Content.Shared.Antag;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Stories.Heretic;

/// <summary>
/// Админ-верб «Сделать еретиком» — как апстримные антаг-вербы: через правило, чтобы выдать цели, роль и влияния.
/// </summary>
public sealed partial class StoriesHereticAdminVerbSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;

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

        // Повторная выдача пересоздала бы компонент и сбросила прогресс.
        if (!HasComp<MindContainerComponent>(args.Target)
            || !TryComp<ActorComponent>(args.Target, out var targetActor)
            || HasComp<StoriesHereticComponent>(args.Target))
            return;

        var target = targetActor.PlayerSession;
        var name = Loc.GetString("stories-heretic-admin-verb-make");
        args.Verbs.Add(new Verb
        {
            Text = name,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_Stories/Heretic/actions.rsi"), "eye"),
            Act = () => _antag.ForceMakeAntag<StoriesHereticRuleComponent>(target, HereticRule, HereticSpecifier),
            Impact = LogImpact.High,
            Message = string.Join(": ", name, Loc.GetString("stories-heretic-admin-verb-make-message")),
        });
    }
}
