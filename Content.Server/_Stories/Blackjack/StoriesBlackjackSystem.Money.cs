using System.Linq;
using Content.Shared._Stories.Blackjack;
using Content.Shared._Stories.Economy.Components;
using Content.Shared.Database;
using Content.Shared.PDA;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Blackjack;

public sealed partial class StoriesBlackjackSystem
{
    private void OnCashier(Entity<BlackjackTableComponent> ent, ref BlackjackCashierMessage args)
    {
        var actor = args.Actor;
        var comp = ent.Comp;
        if (!comp.Wager)
            return;

        if (args.Op is BlackjackCashierOp.SellHeld or BlackjackCashierOp.SellHeldCard)
        {
            SellHeld(ent, actor, args.Op);
            return;
        }

        if (!TryGetSeat(comp, actor, out var index))
        {
            Fail(ent, actor, "not-seated");
            return;
        }

        var seat = comp.Seats[index];
        var amount = args.Amount;
        var op = args.Op;
        if (op != BlackjackCashierOp.Deposit && (amount < 1 || amount > comp.MaxChips))
        {
            Fail(ent, actor, "bad-amount");
            return;
        }

        var buying = false;
        switch (op)
        {
            case BlackjackCashierOp.BuyCash:
                if (CountHeld(actor, comp.CashStack) < amount)
                {
                    Fail(ent, actor, "no-cash");
                    return;
                }

                if (seat.Chips + amount > comp.MaxChips)
                {
                    Fail(ent, actor, "stack-full");
                    return;
                }

                TakeHeld(actor, comp.CashStack, amount);
                seat.Chips += amount;
                buying = true;
                break;
            case BlackjackCashierOp.BuyCard:
                var buyStation = _station.GetOwningStation(ent);
                if (buyStation == null)
                {
                    Fail(ent, actor, "no-station");
                    return;
                }

                if (!TryGetAccount(actor, out var buyAccount))
                {
                    Fail(ent, actor, "no-card");
                    return;
                }

                if (seat.Chips + amount > comp.MaxChips)
                {
                    Fail(ent, actor, "stack-full");
                    return;
                }

                if (!_bank.TryChangeBalance(buyStation.Value, buyAccount, -amount))
                {
                    Fail(ent, actor, "no-funds");
                    return;
                }

                seat.Chips += amount;
                buying = true;
                break;
            case BlackjackCashierOp.Deposit:
                var held = CountHeld(actor, comp.ChipStack);
                if (held <= 0)
                {
                    Fail(ent, actor, "no-chips");
                    return;
                }

                amount = (int) Math.Min(held, comp.MaxChips - seat.Chips);
                if (amount <= 0)
                {
                    Fail(ent, actor, "stack-full");
                    return;
                }

                TakeHeld(actor, comp.ChipStack, amount);
                seat.Chips += amount;
                buying = true;
                break;
            case BlackjackCashierOp.CashOut:
                if (seat.Chips < amount)
                {
                    Fail(ent, actor, "no-stack");
                    return;
                }

                if (!TryGive(actor, comp.CashProto, comp.CashStack, amount))
                {
                    Fail(ent, actor, "no-hand");
                    return;
                }

                seat.Chips -= amount;
                break;
            case BlackjackCashierOp.CashOutCard:
                var outStation = _station.GetOwningStation(ent);
                if (outStation == null)
                {
                    Fail(ent, actor, "no-station");
                    return;
                }

                if (!TryGetAccount(actor, out var outAccount))
                {
                    Fail(ent, actor, "no-card");
                    return;
                }

                if (seat.Chips < amount)
                {
                    Fail(ent, actor, "no-stack");
                    return;
                }

                if (!_bank.TryChangeBalance(outStation.Value, outAccount, amount))
                {
                    Fail(ent, actor, "no-card");
                    return;
                }

                seat.Chips -= amount;
                break;
            case BlackjackCashierOp.Withdraw:
                if (seat.Chips < amount)
                {
                    Fail(ent, actor, "no-stack");
                    return;
                }

                if (!TryGive(actor, comp.ChipsProto, comp.ChipStack, amount))
                {
                    Fail(ent, actor, "no-hand");
                    return;
                }

                seat.Chips -= amount;
                break;
            default:
                return;
        }

        _audio.PlayPvs(comp.ChipSound, ent);
        _popup.PopupEntity(Loc.GetString(buying ? "stories-blackjack-popup-done-buy" : "stories-blackjack-popup-done-cashout", ("amount", amount)), ent, actor);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(actor):player} did {op} for {amount} chips at {ToPrettyString(ent):target}");
        Refresh(ent);
    }

    private void OnOpenCashier(Entity<BlackjackTableComponent> ent, ref BlackjackOpenCashierMessage args)
    {
        if (!ent.Comp.Wager || !TryGetSeat(ent.Comp, args.Actor, out _))
            return;

        _ui.OpenUi(ent.Owner, BlackjackUiKey.Lobby, args.Actor);
    }

    private void SellHeld(Entity<BlackjackTableComponent> ent, EntityUid actor, BlackjackCashierOp op)
    {
        var comp = ent.Comp;
        var held = CountHeld(actor, comp.ChipStack);
        if (held <= 0)
        {
            Fail(ent, actor, "no-chips");
            return;
        }

        if (held > int.MaxValue)
        {
            Fail(ent, actor, "bad-amount");
            return;
        }

        var amount = (int) held;

        if (op == BlackjackCashierOp.SellHeldCard)
        {
            var station = _station.GetOwningStation(ent);
            if (station == null)
            {
                Fail(ent, actor, "no-station");
                return;
            }

            if (!TryGetAccount(actor, out var account))
            {
                Fail(ent, actor, "no-card");
                return;
            }

            if (!_bank.TryChangeBalance(station.Value, account, amount))
            {
                Fail(ent, actor, "no-card");
                return;
            }

            TakeHeld(actor, comp.ChipStack, amount);
        }
        else
        {
            if (!TryFindStack(actor, comp.CashStack, amount, out _) && !_hands.TryGetEmptyHand(actor, out _))
            {
                Fail(ent, actor, "no-hand");
                return;
            }

            TakeHeld(actor, comp.ChipStack, amount);
            if (!TryGive(actor, comp.CashProto, comp.CashStack, amount))
            {
                var cash = SpawnAtPosition(comp.CashProto, Transform(actor).Coordinates);
                _stack.SetCount((cash, null), amount);
            }
        }

        _audio.PlayPvs(comp.ChipSound, ent);
        _popup.PopupEntity(Loc.GetString("stories-blackjack-popup-done-cashout", ("amount", amount)), ent, actor);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(actor):player} did {op} for {amount} held chips at {ToPrettyString(ent):target}");
    }

    private void Fail(Entity<BlackjackTableComponent> ent, EntityUid actor, string key)
    {
        _popup.PopupEntity(Loc.GetString("stories-blackjack-popup-" + key), ent, actor);
    }

    private bool IsLiveStack(EntityUid held, StackComponent stack)
    {
        return stack.Count > 0 && !TerminatingOrDeleted(held) && !EntityManager.IsQueuedForDeletion(held);
    }

    private long CountHeld(EntityUid user, ProtoId<StackPrototype> type)
    {
        long total = 0;
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (TryComp<StackComponent>(held, out var stack) && stack.StackTypeId == type && IsLiveStack(held, stack))
                total += stack.Count;
        }

        return total;
    }

    private void TakeHeld(EntityUid user, ProtoId<StackPrototype> type, int amount)
    {
        var left = amount;
        foreach (var held in _hands.EnumerateHeld(user).ToList())
        {
            if (left <= 0)
                break;

            if (!TryComp<StackComponent>(held, out var stack) || stack.StackTypeId != type || !IsLiveStack(held, stack))
                continue;

            var part = Math.Min(left, stack.Count);
            if (_stack.TryUse((held, null), part))
                left -= part;
        }
    }

    private bool TryFindStack(EntityUid user, ProtoId<StackPrototype> type, int amount, out Entity<StackComponent> found)
    {
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (!TryComp<StackComponent>(held, out var stack) || stack.StackTypeId != type || !IsLiveStack(held, stack))
                continue;

            if ((long) stack.Count + amount > _stack.GetMaxCount(stack))
                continue;

            found = (held, stack);
            return true;
        }

        found = default;
        return false;
    }

    private bool TryGive(EntityUid user, EntProtoId proto, ProtoId<StackPrototype> type, int amount)
    {
        if (TryFindStack(user, type, amount, out var found))
        {
            _stack.SetCount((found.Owner, null), found.Comp.Count + amount);
            return true;
        }

        if (!_hands.TryGetEmptyHand(user, out _))
            return false;

        var spawned = Spawn(proto, Transform(user).Coordinates);
        _stack.SetCount((spawned, null), amount);
        if (_hands.TryPickupAnyHand(user, spawned))
            return true;

        Del(spawned);
        return false;
    }

    private bool TryGetAccount(EntityUid user, out string account)
    {
        account = string.Empty;
        if (!_inventory.TryGetSlotEntity(user, "id", out var slot))
            return false;

        if (TryComp<PdaComponent>(slot, out var pda))
        {
            if (pda.ContainedId == null || !TryComp<IdBankAccountComponent>(pda.ContainedId.Value, out var pdaBank))
                return false;

            account = pdaBank.AccountNumber;
            return true;
        }

        if (!TryComp<IdBankAccountComponent>(slot, out var bank))
            return false;

        account = bank.AccountNumber;
        return true;
    }
}
