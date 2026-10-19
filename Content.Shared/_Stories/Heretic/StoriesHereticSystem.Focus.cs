using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticSystem
{
    private const SlotFlags FocusSlots = SlotFlags.NECK | SlotFlags.EYES | SlotFlags.HEAD | SlotFlags.OUTERCLOTHING;

    private void InitializeFocus()
    {
        SubscribeLocalEvent<StoriesHereticRequiresFocusComponent, ActionAttemptEvent>(OnRequiresFocusAttempt);
        SubscribeLocalEvent<StoriesHereticCodexComponent, UseInHandEvent>(OnCodexUse);
    }

    private void OnRequiresFocusAttempt(Entity<StoriesHereticRequiresFocusComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled || HasFocus(args.User))
            return;

        args.Cancelled = true;
        args.Reason = Loc.GetString("stories-heretic-no-focus");
    }

    private void OnCodexUse(Entity<StoriesHereticCodexComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !HasComp<StoriesHereticComponent>(args.User))
            return;

        args.Handled = true;
        ent.Comp.Open = !ent.Comp.Open;
        Dirty(ent);

        _popup.PopupClient(Loc.GetString(ent.Comp.Open ? "stories-heretic-codex-open" : "stories-heretic-codex-close"),
            args.User,
            args.User);
    }

    public bool HasFocus(EntityUid uid)
    {
        if (TryComp<StoriesHereticComponent>(uid, out var heretic) && heretic.Ascended)
            return true;

        if (HasOpenCodex(uid))
            return true;

        var slots = _inventory.GetSlotEnumerator(uid, FocusSlots);
        while (slots.NextItem(out var item))
        {
            if (HasComp<StoriesHereticFocusComponent>(item))
                return true;
        }

        return false;
    }

    public bool HasOpenCodex(EntityUid uid)
    {
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            if (TryComp<StoriesHereticCodexComponent>(held, out var codex) && codex.Open)
                return true;
        }

        return false;
    }

    public int GetInfluenceMultiplier(EntityUid uid)
    {
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            if (TryComp<StoriesHereticCodexComponent>(held, out var codex) && codex.Open)
                return codex.InfluenceMultiplier;
        }

        return 1;
    }
}
