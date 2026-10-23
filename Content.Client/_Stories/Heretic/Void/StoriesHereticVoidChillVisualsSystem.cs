using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Void;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidChillVisualsSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public const string LayerKey = "STHereticVoidChill";
    public static readonly ResPath Rsi = new("/Textures/_Stories/Heretic/Effects/void_chill.rsi");
    public static readonly Color Tint = Color.FromHex("#9fd8ff");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticVoidChillComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoriesHereticVoidChillComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<StoriesHereticVoidChillComponent, ComponentRemove>(OnRemove);
    }

    private void OnStartup(Entity<StoriesHereticVoidChillComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnState(Entity<StoriesHereticVoidChillComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh(ent);
    }

    private void OnRemove(Entity<StoriesHereticVoidChillComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.RemoveLayer((ent.Owner, sprite), LayerKey, false);
        _sprite.SetColor((ent.Owner, sprite), Color.White);
    }

    private void Refresh(Entity<StoriesHereticVoidChillComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var uid = (ent.Owner, sprite);
        _sprite.SetColor(uid, Tint);

        if (_player.LocalEntity is not { } local || !HasComp<StoriesHereticComponent>(local))
        {
            _sprite.RemoveLayer(uid, LayerKey, false);
            return;
        }

        var state = ent.Comp.Stacks >= ent.Comp.MaxStacks ? "full" : "partial";
        if (_sprite.LayerMapTryGet(uid, LayerKey, out _, false))
        {
            _sprite.LayerSetRsiState(uid, LayerKey, state);
            return;
        }

        var layer = _sprite.AddLayer(uid, new SpriteSpecifier.Rsi(Rsi, state), 0);
        _sprite.LayerMapSet(uid, LayerKey, layer);
    }
}
