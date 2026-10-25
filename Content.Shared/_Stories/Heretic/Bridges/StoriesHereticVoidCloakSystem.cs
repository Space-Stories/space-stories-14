using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Strip;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._Stories.Heretic.Bridges;

public sealed partial class StoriesHereticVoidCloakSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticVoidCloakComponent, ToggleClothingEvent>(OnToggle,
            before: new[] { typeof(ToggleableClothingSystem) });
        SubscribeLocalEvent<StoriesHereticVoidCloakComponent, StoriesStripItemHiddenEvent>(OnStripHidden);
        SubscribeLocalEvent<StoriesHereticVoidCloakHoodComponent, GotEquippedEvent>(OnHoodEquipped);
        SubscribeLocalEvent<StoriesHereticVoidCloakHoodComponent, GotUnequippedEvent>(OnHoodUnequipped);
    }

    private void OnToggle(Entity<StoriesHereticVoidCloakComponent> ent, ref ToggleClothingEvent args)
    {
        if (args.Handled || HasComp<StoriesHereticComponent>(args.Performer))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("stories-heretic-void-cloak-refuse"), args.Performer, args.Performer);
    }

    private void OnStripHidden(Entity<StoriesHereticVoidCloakComponent> ent, ref StoriesStripItemHiddenEvent args)
    {
        if (ent.Comp.Hidden && args.Viewer != Transform(ent).ParentUid)
            args.Hidden = true;
    }

    private void OnHoodEquipped(Entity<StoriesHereticVoidCloakHoodComponent> ent, ref GotEquippedEvent args)
    {
        SetHidden(ent, true);
    }

    private void OnHoodUnequipped(Entity<StoriesHereticVoidCloakHoodComponent> ent, ref GotUnequippedEvent args)
    {
        SetHidden(ent, false);
    }

    private void SetHidden(EntityUid hood, bool hidden)
    {
        if (_net.IsClient
            || !TryComp<AttachedClothingComponent>(hood, out var attached)
            || !TryComp<StoriesHereticVoidCloakComponent>(attached.AttachedUid, out var cloak)
            || cloak.Hidden == hidden)
            return;

        cloak.Hidden = hidden;
        Dirty(attached.AttachedUid, cloak);
    }
}
