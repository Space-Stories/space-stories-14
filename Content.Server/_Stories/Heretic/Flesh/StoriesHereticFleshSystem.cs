using Content.Server._Stories.Heretic.Ghouls;
using Content.Server.Popups;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Stories.Heretic.Flesh;

public sealed partial class StoriesHereticFleshSystem : EntitySystem
{
    [Dependency] private StoriesHereticGhoulSystem _ghouls = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticGraspGhoulComponent, StoriesHereticGraspHitEvent>(OnGraspGhoul);
    }

    private void OnGraspGhoul(Entity<StoriesHereticGraspGhoulComponent> ent, ref StoriesHereticGraspHitEvent args)
    {
        // По живым Хватка работает как обычно, гулем становится только труп.
        if (!_mobState.IsDead(args.Target))
            return;

        var limit = ent.Comp.Limit + (TryComp<StoriesHereticComponent>(ent, out var heretic) && heretic.Ascended ? ent.Comp.AscendedBonus : 0);
        if (_ghouls.TryRaise(ent, args.Target, ent.Comp.Conversion, limit, out var reason))
            _popup.PopupEntity(Loc.GetString("stories-heretic-flesh-ghoul-raised", ("target", args.Target)), args.Target, ent);
        else
            _popup.PopupEntity(reason, args.Target, ent);
    }
}
