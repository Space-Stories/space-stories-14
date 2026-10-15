using Content.Shared._Stories.Heretic.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Stories.Heretic;

/// <summary>
/// Призыв, ждущий игрока, полупрозрачен.
/// </summary>
public sealed partial class StoriesHereticSummonWaitingVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    // ComponentInit/Remove, а не Startup/Shutdown: на них уже подписана общая система.
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticSummonWaitingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StoriesHereticSummonWaitingComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<StoriesHereticSummonWaitingComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // Прежний цвет спрайта (окраска моба) вернётся, когда роль займут.
        ent.Comp.OriginalColor = sprite.Color;
        _sprite.SetColor((ent.Owner, sprite), sprite.Color.WithAlpha(sprite.Color.A * 0.5f));
    }

    private void OnRemove(Entity<StoriesHereticSummonWaitingComponent> ent, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(ent) && ent.Comp.OriginalColor is { } color && TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetColor((ent.Owner, sprite), color);
    }
}
