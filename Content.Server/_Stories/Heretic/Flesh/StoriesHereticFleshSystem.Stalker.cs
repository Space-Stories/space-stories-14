using Content.Server.Polymorph.Systems;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared._Stories.Heretic.Flesh;
using Content.Shared.Emp;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Server._Stories.Heretic.Flesh;

// Сталкер: ЭМИ и одноразово выбираемый облик.
public sealed partial class StoriesHereticFleshSystem
{
    [Dependency] private SharedEmpSystem _emp = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private void InitializeStalker()
    {
        SubscribeLocalEvent<StoriesHereticStalkerFormComponent, StoriesHereticEldritchEmpEvent>(OnStalkerEmp);
        SubscribeLocalEvent<StoriesHereticStalkerFormComponent, StoriesHereticStalkerShapeshiftEvent>(OnStalkerShapeshift);
        SubscribeLocalEvent<StoriesHereticStalkerFormComponent, StoriesHereticStalkerFormSelectMessage>(OnStalkerFormSelected);
    }

    private void OnStalkerEmp(Entity<StoriesHereticStalkerFormComponent> ent, ref StoriesHereticEldritchEmpEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _emp.EmpPulse(Transform(ent).Coordinates, args.Range, args.Energy, args.Duration, ent);
    }

    private void OnStalkerShapeshift(Entity<StoriesHereticStalkerFormComponent> ent, ref StoriesHereticStalkerShapeshiftEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.Chosen is { } chosen)
            _polymorph.PolymorphEntity(ent, chosen);
        else
            _ui.OpenUi(ent.Owner, StoriesHereticStalkerFormUiKey.Key, ent);
    }

    private void OnStalkerFormSelected(Entity<StoriesHereticStalkerFormComponent> ent, ref StoriesHereticStalkerFormSelectMessage args)
    {
        // Выбор один раз и только из списка компонента.
        var chosen = args.Polymorph;
        if (ent.Comp.Chosen != null || !ent.Comp.Forms.Exists(f => f.Polymorph == chosen))
            return;

        ent.Comp.Chosen = chosen;
        Dirty(ent);
        _ui.CloseUi(ent.Owner, StoriesHereticStalkerFormUiKey.Key);
        _polymorph.PolymorphEntity(ent, chosen);
    }
}
