using Content.Client.Damage;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Stories.DamageVisuals;

/// <summary>
/// Перекрашивает наложения урона (кровь, ожоги) под расу, когда она меняется на лету:
/// пульт MIMIC-9, генокрад. Апстрим задаёт цвет этих слоёв один раз при создании персонажа,
/// и слайм в облике человека продолжал бы кровить зелёным.
/// </summary>
public sealed partial class SpeciesDamageOverlaySystem : EntitySystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, AfterAutoHandleStateEvent>(OnProfileState);
    }

    private void OnProfileState(Entity<HumanoidProfileComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<DamageVisualsComponent>(ent, out var visuals) || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!ProtoMan.Resolve(ent.Comp.Species, out var species)
            || !ProtoMan.Index(species.Prototype).TryGetComponent<DamageVisualsComponent>(out var speciesVisuals, _factory)
            || speciesVisuals.DamageOverlayGroups == null)
            return;

        // Ключи слоёв те же, что выдаёт DamageVisualsSystem при создании персонажа.
        foreach (var (group, overlay) in speciesVisuals.DamageOverlayGroups)
        {
            if (overlay.Color == null)
                continue;

            var color = Color.FromHex(overlay.Color);
            foreach (var layer in visuals.TargetLayerMapKeys)
            {
                SetColor((ent, sprite), $"{layer}{group}", color);
            }

            SetColor((ent, sprite), $"DamageOverlay{group}", color);
        }
    }

    private void SetColor(Entity<SpriteComponent?> sprite, string key, Color color)
    {
        if (_sprite.LayerMapTryGet(sprite, key, out var index, false))
            _sprite.LayerSetColor(sprite, index, color);
    }
}
