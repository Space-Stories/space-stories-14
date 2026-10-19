using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Stories.Heretic;

public sealed partial class StoriesHereticMarkVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public const string LayerKey = "STHereticMark";
    public static readonly ResPath Rsi = new("/Textures/_Stories/Heretic/marks.rsi");

    public static readonly IReadOnlyDictionary<ProtoId<TagPrototype>, string> States = new Dictionary<ProtoId<TagPrototype>, string>
    {
        ["STHereticPathFlesh"] = "emark1",
        ["STHereticPathVoid"] = "emark2",
        ["STHereticPathAsh"] = "emark3",
        ["STHereticPathRust"] = "emark4",
        ["STHereticPathBlade"] = "emark5",
        ["STHereticPathCosmos"] = "emark6",
        ["STHereticPathLock"] = "emark7",
        ["STHereticPathMoon"] = "emark8",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticMarkComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoriesHereticMarkComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<StoriesHereticMarkComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<StoriesHereticMarkComponent> ent, ref ComponentStartup args)
    {
        Update(ent);
    }

    private void OnState(Entity<StoriesHereticMarkComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Update(ent);
    }

    private void OnShutdown(Entity<StoriesHereticMarkComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.RemoveLayer((ent.Owner, sprite), LayerKey, false);
    }

    private void Update(Entity<StoriesHereticMarkComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var uid = (ent.Owner, sprite);
        if (!States.TryGetValue(ent.Comp.Path, out var state))
        {
            _sprite.RemoveLayer(uid, LayerKey, false);
            return;
        }

        if (_sprite.LayerMapTryGet(uid, LayerKey, out var layer, false))
        {
            _sprite.LayerSetRsiState(uid, LayerKey, state);
            return;
        }

        layer = _sprite.AddLayer(uid, new SpriteSpecifier.Rsi(Rsi, state), 0);
        _sprite.LayerMapSet(uid, LayerKey, layer);
    }
}
