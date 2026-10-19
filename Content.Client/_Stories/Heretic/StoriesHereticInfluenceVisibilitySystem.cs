using Content.Shared._Stories.Heretic.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Stories.Heretic;

public sealed partial class StoriesHereticInfluenceVisibilitySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticInfluenceComponent, ComponentStartup>(OnInfluenceStartup);
        SubscribeLocalEvent<StoriesHereticComponent, ComponentStartup>(OnHereticStartup);
        SubscribeLocalEvent<StoriesHereticComponent, ComponentShutdown>(OnHereticShutdown);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnInfluenceStartup(Entity<StoriesHereticInfluenceComponent> ent, ref ComponentStartup args)
    {
        _sprite.SetVisible(ent.Owner, IsLocalHeretic());
    }

    private void OnHereticStartup(Entity<StoriesHereticComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner)
            SetAllVisible(true);
    }

    private void OnHereticShutdown(Entity<StoriesHereticComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            SetAllVisible(false);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        SetAllVisible(IsLocalHeretic());
    }

    private bool IsLocalHeretic()
    {
        return _player.LocalEntity is { } local && HasComp<StoriesHereticComponent>(local);
    }

    private void SetAllVisible(bool visible)
    {
        var query = EntityQueryEnumerator<StoriesHereticInfluenceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            _sprite.SetVisible((uid, sprite), visible);
        }
    }
}
