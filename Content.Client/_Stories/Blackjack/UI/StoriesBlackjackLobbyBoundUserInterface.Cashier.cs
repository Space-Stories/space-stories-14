using Content.Shared._Stories.Blackjack;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;

namespace Content.Client._Stories.Blackjack.UI;

public sealed partial class StoriesBlackjackLobbyBoundUserInterface
{
    private const int DefaultAmount = 50;

    private SharedHandsSystem? _hands;
    private int _amount = DefaultAmount;
    private int _cash;
    private int _items;

    private void ConnectCashier(StoriesBlackjackCashier cashier)
    {
        cashier.SetAmountText(_amount.ToString());
        cashier.OnChip += OnCashierChip;
        cashier.OnStep += OnCashierStep;
        cashier.OnReset += OnCashierReset;
        cashier.OnAmountChanged += OnCashierAmountChanged;
        cashier.OnOp += SendCashier;
        cashier.OnCashOutAll += OnCashierCashOutAll;
    }

    private void UpdateCashier(BlackjackTableComponent table, int mine)
    {
        if (_window == null)
            return;

        var cashier = _window.Cashier;
        if (!table.Wager)
        {
            cashier.SetMode(StoriesBlackjackCashierMode.Hidden);
            return;
        }

        if (mine < 0)
        {
            if (_items > 0)
            {
                cashier.SetMode(StoriesBlackjackCashierMode.Held);
                cashier.SetHeldOnly(Loc.GetString("stories-blackjack-cashier-held-only", ("count", _items)));
            }
            else
            {
                cashier.SetMode(StoriesBlackjackCashierMode.Locked);
            }

            return;
        }

        cashier.SetMode(StoriesBlackjackCashierMode.Seated);
        cashier.SetSeated(BuildCashierView(table, table.Seats[mine].Chips));
    }

    private StoriesBlackjackCashierView BuildCashierView(BlackjackTableComponent table, int chips)
    {
        var amount = _amount;
        var valid = amount >= 1 && amount <= table.MaxChips;
        var room = chips + amount <= table.MaxChips;
        var hasCash = _cash >= amount;
        var fits = valid && amount <= chips;

        var amountHint = string.Empty;
        if (!valid)
            amountHint = Loc.GetString("stories-blackjack-cashier-hint-amount", ("max", table.MaxChips));
        else if (amount >= table.MaxChips)
            amountHint = Loc.GetString("stories-blackjack-cashier-hint-max");

        var buyHint = string.Empty;
        if (valid && !room)
            buyHint = Loc.GetString("stories-blackjack-cashier-hint-full");
        else if (valid && !hasCash)
            buyHint = Loc.GetString("stories-blackjack-cashier-hint-cash");

        var cashOutHint = string.Empty;
        if (chips <= 0)
            cashOutHint = Loc.GetString("stories-blackjack-cashier-hint-no-chips");
        else if (valid && !fits)
            cashOutHint = Loc.GetString("stories-blackjack-cashier-hint-few");

        return new StoriesBlackjackCashierView(
            Chips: chips.ToString(),
            Cash: _cash.ToString(),
            Held: _items.ToString(),
            ShowHeld: _items > 0,
            CanChip: amount < table.MaxChips,
            CanMinus: amount > 0,
            CanReset: amount > 0,
            CanBuyCash: valid && room && hasCash,
            CanBuyCard: valid && room,
            BuyHint: buyHint,
            CanCashOut: fits,
            CanCashOutCard: fits,
            CanCashOutAll: chips > 0,
            CashOutHint: cashOutHint,
            CanWithdraw: fits,
            BuyPrimary: chips < table.MinBet,
            AmountHint: amountHint);
    }

    private void ReadHeld(BlackjackTableComponent table, out int cash, out int items)
    {
        cash = 0;
        items = 0;
        if (_player.LocalEntity is not { } local || !EntMan.TryGetComponent(local, out HandsComponent? hands))
            return;

        _hands ??= EntMan.System<SharedHandsSystem>();
        foreach (var hand in hands.SortedHands)
        {
            if (!_hands.TryGetHeldItem((local, hands), hand, out var held) || !EntMan.TryGetComponent(held, out StackComponent? stack))
                continue;

            if (stack.StackTypeId == table.CashStack)
                cash += stack.Count;
            else if (stack.StackTypeId == table.ChipStack)
                items += stack.Count;
        }
    }

    private void UpdateHeld()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table) || !table.Wager)
            return;

        ReadHeld(table, out var cash, out var items);
        if (cash == _cash && items == _items)
            return;

        _cash = cash;
        _items = items;
        UpdateCashier(table, _mine);
    }

    private void RefreshCashier()
    {
        if (EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table))
            UpdateCashier(table, _mine);
    }

    private void SetAmount(int value)
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table))
            return;

        _amount = Math.Clamp(value, 0, table.MaxChips);
        _window.Cashier.SetAmountText(_amount.ToString());
        UpdateCashier(table, _mine);
    }

    private void OnCashierChip(int value)
    {
        SetAmount(_amount + value);
    }

    private void OnCashierStep(int direction)
    {
        if (EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table))
            SetAmount(_amount + direction * table.MinBet);
    }

    private void OnCashierReset()
    {
        SetAmount(0);
    }

    private void OnCashierAmountChanged()
    {
        _amount = _window != null && int.TryParse(_window.Cashier.GetAmountText(), out var amount) ? amount : 0;
        RefreshCashier();
    }

    private void OnCashierCashOutAll()
    {
        if (_mine < 0 || !EntMan.TryGetComponent(Owner, out BlackjackTableComponent? table))
            return;

        var chips = table.Seats[_mine].Chips;
        if (chips > 0)
            SendMessage(new BlackjackCashierMessage(BlackjackCashierOp.CashOut, chips));
    }

    private void SendCashier(BlackjackCashierOp op)
    {
        if (op is BlackjackCashierOp.Deposit or BlackjackCashierOp.SellHeld or BlackjackCashierOp.SellHeldCard)
        {
            SendMessage(new BlackjackCashierMessage(op, 0));
            return;
        }

        if (_amount >= 1)
            SendMessage(new BlackjackCashierMessage(op, _amount));
    }
}
