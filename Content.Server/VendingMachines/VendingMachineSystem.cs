// Stories-Economy
using System.Linq;
using System.Numerics;
using Content.Server._Stories.Economy;
using Content.Server.Cargo.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.VendingMachines.Components;
using Content.Server.Access.Components;
using Content.Server.Vocalization.Systems;
using Content.Shared._Stories.Economy.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.PDA;
using Content.Shared.Power;
using Content.Shared.Roles;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Content.Shared.Wall;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineSystem : SharedVendingMachineSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private ThrowingSystem _throwingSystem = default!;
    [Dependency] private BankSystem _bank = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private TagSystem _tags = default!;

    private const float WallVendEjectDistanceFromWall = 1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, BeforeActivatableUIOpenEvent>(
            (uid, comp, args) => UpdateVendingUI(uid, args.User, comp));
        SubscribeLocalEvent<VendingMachineComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<BankBalanceChangedEventArgs>(OnBalanceChanged);
    }

    protected override bool ShouldThrowVendItem(Entity<VendingMachineEjectComponent> entity)
    {
        return HasComp<VendingMachineShootComponent>(entity.Owner);
    }

    protected override void EjectItem(Entity<VendingMachineComponent?, VendingMachineEjectComponent?> entity, bool forceEject = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp1, ref entity.Comp2))
            return;

        var uid = entity.Owner;
        var ejectComponent = entity.Comp2;

        if (ejectComponent.NextItemToEject is not { } item)
        {
            ejectComponent.ThrowNextItem = false;
            return;
        }

        // Default spawn coordinates
        var xform = Transform(uid);
        var spawnCoordinates = xform.Coordinates;

        // Make sure the wallvends spawn outside of the wall.
        if (TryComp<WallMountComponent>(uid, out var wallMountComponent))
        {
            var offset = (wallMountComponent.Direction + xform.LocalRotation - Math.PI / 2).ToVec() * WallVendEjectDistanceFromWall;
            spawnCoordinates = spawnCoordinates.Offset(offset);
        }

        var ent = Spawn(item, spawnCoordinates);

        if (ejectComponent.ThrowNextItem)
        {
            var range = ejectComponent.NonLimitedEjectRange;
            var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
            _throwingSystem.TryThrow(ent, direction, ejectComponent.NonLimitedEjectForce);
        }

        ejectComponent.NextItemToEject = null;
        ejectComponent.ThrowNextItem = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var dispenseOnHitQuery = EntityQueryEnumerator<VendingMachineDispenseOnHitComponent>();
        while (dispenseOnHitQuery.MoveNext(out _, out var dispenseOnHit))
        {
            if (dispenseOnHit.NextDispenseTime is not { } nextDispenseTime || curTime <= nextDispenseTime)
                continue;

            dispenseOnHit.NextDispenseTime = null;
        }

        var disabled = EntityQueryEnumerator<EmpDisabledComponent, VendingMachineComponent, VendingMachineEjectComponent>();
        while (disabled.MoveNext(out var uid, out _, out var comp, out var eject))
        {
            if (eject.NextEmpEject >= curTime) continue;

            EjectRandom((uid, comp, eject), true, false);
            eject.NextEmpEject += (5 * eject.EjectDelay);
        }
    }

    [SubscribeLocalEvent]
    private void OnVendingPrice(Entity<VendingMachineComponent> entity, ref PriceCalculationEvent args)
    {
        var price = 0.0;

        foreach (var entry in entity.Comp.Inventory.Values)
        {
            if (!ProtoMan.TryIndex<EntityPrototype>(entry.ID, out var proto))
            {
                Log.Error($"Unable to find entity prototype {entry.ID} on {ToPrettyString(entity)} vending.");
                continue;
            }

            price += entry.Amount * _pricing.GetEstimatedPrice(proto);
        }

        args.Price += price;
    }

    [SubscribeLocalEvent]
    private void OnDamageChanged(Entity<VendingMachineComponent> entity, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased && entity.Comp.Broken)
        {
            entity.Comp.Broken = false;
            Dirty(entity);
            return;
        }

        if (!TryComp<VendingMachineDispenseOnHitComponent>(entity.Owner, out var dispenseOnHit))
            return;

        if (entity.Comp.Broken || dispenseOnHit.CoolingDown || args.DamageDelta == null)
            return;

        if (!(args.DamageIncreased && args.DamageDelta.GetTotal() >= dispenseOnHit.Threshold) ||
            !_random.Prob(dispenseOnHit.Chance)) return;

        if (dispenseOnHit.NextDispenseDelay != null)
        {
            dispenseOnHit.NextDispenseTime = Timing.CurTime + dispenseOnHit.NextDispenseDelay.Value;
        }

        EjectRandom((entity.Owner, entity.Comp), throwItem: true, forceEject: true);
    }

    [SubscribeLocalEvent]
    private void OnSelfDispense(Entity<VendingMachineComponent> entity, ref VendingMachineSelfDispenseEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        EjectRandom((entity.Owner, entity.Comp), throwItem: true, forceEject: false);
    }

    [SubscribeLocalEvent]
    private void OnPriceCalculation(Entity<VendingMachineRestockComponent> entity, ref PriceCalculationEvent args)
    {
        List<double> priceSets = new();

        // Find the most expensive inventory and use that as the highest price.
        foreach (var vendingInventory in entity.Comp.CanRestock)
        {
            double total = 0;

            if (ProtoMan.TryIndex(vendingInventory, out VendingMachineInventoryPrototype? inventoryPrototype))
            {
                foreach (var (item, amount) in inventoryPrototype.StartingInventory)
                {
                    if (ProtoMan.TryIndex(item, out EntityPrototype? prototype))
                        total += _pricing.GetEstimatedPrice(prototype) * amount;
                }
            }

            priceSets.Add(total);
        }

        if (priceSets.Any())
            args.Price += priceSets.Max();
    }

    [SubscribeLocalEvent]
    private void OnTryVocalize(Entity<VendingMachineComponent> ent, ref TryVocalizeEvent args)
    {
        args.Cancelled |= ent.Comp.Broken;
    }

    public void SetShooting(Entity<VendingMachineEjectComponent?> entity, bool canShoot)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (canShoot)
            EnsureComp<VendingMachineShootComponent>(entity.Owner);
        else
            RemComp<VendingMachineShootComponent>(entity.Owner);
    }

    /// <summary>
    /// Sets the <see cref="VendingMachineComponent.Contraband"/> property of the vending machine.
    /// </summary>
    public void SetContraband(Entity<VendingMachineComponent> entity, bool contraband)
    {
        entity.Comp.Contraband = contraband;
        Dirty(entity);
    }

    /// <summary>
    /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
    /// </summary>
    public void EjectRandom(
        Entity<VendingMachineComponent?, VendingMachineEjectComponent?> entity,
        bool throwItem,
        bool forceEject = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp1, ref entity.Comp2))
            return;

        var uid = entity.Owner;
        var vendComponent = entity.Comp1;
        var ejectComponent = entity.Comp2;
        var availableItems = GetAvailableInventory(uid, vendComponent);
        if (availableItems.Count <= 0)
            return;

        var item = _random.Pick(availableItems);

        if (forceEject)
        {
            ejectComponent.NextItemToEject = item.ID;
            ejectComponent.ThrowNextItem = throwItem;
            var entry = GetEntry(uid, item.ID, item.Type, vendComponent);
            if (entry != null)
            {
                entry.Amount--;
                Dirty(uid, vendComponent);
                UpdateUI((uid, vendComponent));
            }

            EjectItem((uid, vendComponent, ejectComponent), forceEject);
        }
        else
        {
            TryEjectVendorItem(uid, item.Type, item.ID, throwItem, user: null, vendComponent: vendComponent, ejectComponent: ejectComponent);
        }
    }

    /// <summary>
    /// Checks if the user gets free items from this vending machine.
    /// Returns true if the user's ID card has a PresetIdCardComponent and its JobName is in the machine's FreeJobs list.
    /// Returns true if the user or their ID card has a tag from the machine's FreeTags list.
    /// </summary>
    private bool IsFreeForUser(EntityUid uid, EntityUid user, VendingMachineComponent component)
    {
        if (component.FreeJobs.Count == 0 && component.FreeTags.Count == 0)
            return false;

        if (_tags.HasAnyTag(user, component.FreeTags))
            return true;

        if (!_idCard.TryFindIdCard(user, out var idCard))
            return false;
        
        if (_tags.HasAnyTag(idCard, component.FreeTags))
            return true;

        ProtoId<JobPrototype>? jobId = null;
        if (idCard.Comp.JobPrototype != null)
        {
            jobId = idCard.Comp.JobPrototype;
        }
        else if (TryComp<PresetIdCardComponent>(idCard, out var preset) && preset.JobName != null)
        {
            jobId = preset.JobName;
        }

        if (jobId == null)
            return false;

        foreach (var freeJob in component.FreeJobs)
        {
            if (freeJob.Id == jobId.Value.Id)
                return true;
        }

        return false;
    }

    private void UpdateVendingUI(EntityUid uid, EntityUid user, VendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component)) return;

        var authorized = IsAuthorized(uid, user, component);
        var inventory = GetAllInventory(uid, component);

        // If the machine is freeVend or user is authorized via access card/job, show prices as 0
        if (component.FreeVend || IsFreeForUser(uid, user, component))
        {
            var freeInventory = new List<VendingMachineInventoryEntry>();
            foreach (var entry in inventory)
                freeInventory.Add(new VendingMachineInventoryEntry(entry.Type, entry.ID, entry.Amount, 0));
            inventory = freeInventory;
        }

        var state = new VendingMachineUIState(inventory, authorized);
        UISystem.SetUiState(uid, VendingMachineUiKey.Key, state);

        SendBalanceToUser(uid, user, component);
    }

    protected override void UpdateUI(Entity<VendingMachineComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp)) return;

        var actors = UISystem.GetActors(entity.Owner, VendingMachineUiKey.Key).ToList();
        if (actors.Count == 0) return;

        // Re-use UpdateVendingUI so each actor gets correct (possibly free) inventory
        foreach (var actor in actors)
        {
            UpdateVendingUI(entity.Owner, actor, entity.Comp);
        }
    }

    private bool TryGetAccountNumber(EntityUid user, out string accountNumber)
    {
        accountNumber = string.Empty;
        if (_idCard.TryFindIdCard(user, out var idCard) && TryComp<IdBankAccountComponent>(idCard, out var bankComp))
        {
            accountNumber = bankComp.AccountNumber;
            return true;
        }
        return false;
    }

    private void OnBoundUIOpened(EntityUid uid, VendingMachineComponent component, BoundUIOpenedEvent args)
    {
        if (args.UiKey is not VendingMachineUiKey key || key != VendingMachineUiKey.Key) return;
        UpdateVendingUI(uid, args.Actor, component);
    }

    private void SendBalanceToUser(EntityUid machine, EntityUid user, VendingMachineComponent component)
    {
        var station = _station.GetOwningStation(machine);
        int? balance = null;

        if (station != null && TryGetAccountNumber(user, out var accountNumber) && _bank.TryGetAccount(station.Value, accountNumber, out var account))
            balance = account.Balance;

        UISystem.ServerSendUiMessage(machine, VendingMachineUiKey.Key, new VendingMachineBalanceMessage(balance), user);
    }

    public override void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component)
    {
        if (component.Broken || !_power.IsPowered(uid))
            return;

        if (!TryComp<VendingMachineEjectComponent>(uid, out var ejectComponent))
            return;

        if (ejectComponent.Ejecting)
            return;

        if (!IsAuthorized(uid, sender, component))
        {
            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, sender);
            Deny((uid, component), sender, ejectComponent);
            return;
        }

        var entry = GetEntry(uid, itemId, type, component);

        if (entry == null || entry.Amount <= 0)
        {
            Deny((uid, component), sender, ejectComponent);
            return;
        }

        if (entry.Price > 0 && !component.FreeVend)
        {
            // Free for authorized department staff only
            if (!IsFreeForUser(uid, sender, component) &&
                !TryProcessPayment(sender, uid, component, (int)entry.Price, itemId, !component.DisableFinancialLogging))
            {
                Popup.PopupEntity(Loc.GetString("stories-vending-machine-insufficient-funds"), uid, sender);
                Deny((uid, component), sender, ejectComponent);
                return;
            }
        }

        TryEjectVendorItem(uid, type, itemId, ShouldThrowVendItem((uid, ejectComponent)), sender, component, ejectComponent);
    }

    private bool TryProcessPayment(EntityUid user, EntityUid machine, VendingMachineComponent component, int amount, string itemId, bool logTrans)
    {
        var station = _station.GetOwningStation(machine);
        if (station == null) return false;

        string itemName = ProtoMan.HasIndex<EntityPrototype>(itemId) ? ProtoMan.Index<EntityPrototype>(itemId).Name : itemId;
        string machineName = Name(machine);

        if (TryGetAccountNumber(user, out var userAcc))
        {
            if (_bank.TryChangeBalance(station.Value, userAcc, -amount))
            {
                if (logTrans)
                    _bank.LogTransaction(station.Value, userAcc, machineName, amount, Loc.GetString("stories-bank-log-purchase", ("item", itemName)));

                SendBalanceToUser(machine, user, component);
                return true;
            }
        }

        return false;
    }

    private void OnBalanceChanged(BankBalanceChangedEventArgs ev)
    {
        var query = EntityQueryEnumerator<VendingMachineComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            foreach (var actor in UISystem.GetActors(uid, VendingMachineUiKey.Key))
            {
                if (TryGetAccountNumber(actor, out var accNum) && accNum == ev.AccountNumber)
                {
                    SendBalanceToUser(uid, actor, component);
                }
            }
        }
    }
}
