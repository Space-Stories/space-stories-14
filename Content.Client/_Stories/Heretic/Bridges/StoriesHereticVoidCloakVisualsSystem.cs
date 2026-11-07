using Content.Client.Clothing;
using Content.Shared._Stories.Heretic.Bridges;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Item;

namespace Content.Client._Stories.Heretic.Bridges;

public sealed partial class StoriesHereticVoidCloakVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticVoidCloakComponent, GetEquipmentVisualsEvent>(OnGetVisuals,
            after: new[] { typeof(ClientClothingSystem) });
        SubscribeLocalEvent<StoriesHereticVoidCloakComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnGetVisuals(Entity<StoriesHereticVoidCloakComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.Hidden)
            args.Layers.Clear();
    }

    private void OnState(Entity<StoriesHereticVoidCloakComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<ClothingComponent>(ent, out var clothing) || clothing.InSlot == null)
            return;

        RaiseLocalEvent(Transform(ent).ParentUid, new VisualsChangedEvent(GetNetEntity(ent), clothing.InSlot));
    }
}
