using Content.Shared._Stories.Heretic.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Stories.Heretic;

/// <summary>
/// Печать пути под помеченной целью: слой спрайта с RSI <c>marks.rsi</c>.
/// </summary>
public sealed partial class StoriesHereticMarkVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public const string LayerKey = "STHereticMark";
    public static readonly ResPath Rsi = new("/Textures/_Stories/Heretic/marks.rsi");

    // Ключ — ID тега пути. Номера выбраны по виду печатей в tg eldritch.dmi (порядок tg: Плоть, Пустота, Пепел, Ржавчина, Клинок, Космос, Замок, Луна).
    public static readonly IReadOnlyDictionary<string, string> States = new Dictionary<string, string>
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
        if (!TryComp<SpriteComponent>(ent, out var sprite) || !States.TryGetValue(ent.Comp.Path, out var state))
            return;

        var uid = (ent.Owner, sprite);
        if (_sprite.LayerMapTryGet(uid, LayerKey, out var layer, false))
        {
            _sprite.LayerSetRsiState(uid, LayerKey, state);
            return;
        }

        // Индекс 0: печать рисуется под моб-слоями.
        layer = _sprite.AddLayer(uid, new SpriteSpecifier.Rsi(Rsi, state), 0);
        _sprite.LayerMapSet(uid, LayerKey, layer);
    }
}
