using Content.Client.UserInterface.Controls;
using Content.Shared._Stories.Heretic.Flesh;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.Polymorph;

namespace Content.Client._Stories.Heretic.UI;

/// <summary>Радиальное меню выбора облика Сталкера.</summary>
[UsedImplicitly]
public sealed class StoriesHereticStalkerFormBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        if (EntMan.TryGetComponent<StoriesHereticStalkerFormComponent>(Owner, out var form))
        {
            var buttons = new List<RadialMenuOptionBase>();
            foreach (var option in form.Forms)
            {
                buttons.Add(new RadialMenuActionOption<ProtoId<PolymorphPrototype>>(Select, option.Polymorph)
                {
                    IconSpecifier = RadialMenuIconSpecifier.With(option.Mob),
                    ToolTip = Loc.GetString(option.Name),
                });
            }

            _menu.SetButtons(buttons);
        }

        _menu.OpenOverMouseScreenPosition();
    }

    private void Select(ProtoId<PolymorphPrototype> polymorph)
    {
        SendPredictedMessage(new StoriesHereticStalkerFormSelectMessage(polymorph));
    }
}
