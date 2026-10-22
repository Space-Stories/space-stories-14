using Content.Server.PDA;
using Content.Shared._Stories.DisguiseRemote;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Changeling.Systems;
using Content.Shared.Cloning;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mindshield;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Server._Stories.DisguiseRemote;

public sealed partial class DisguiseRemoteSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedChangelingIdentitySystem _changelingIdentity = default!;
    [Dependency] private SharedCloningSystem _cloning = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private PdaSystem _pda = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisguiseRemoteComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DisguisedComponent, DisguiseToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<DisguisedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DisguisedComponent, GetMindShieldStatusEvent>(OnMindShieldStatus, after: [typeof(InventorySystem)]);
    }

    private void OnAfterInteract(Entity<DisguiseRemoteComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target || target == args.User)
            return;

        if (!HasComp<HumanoidProfileComponent>(target) || !HasComp<HumanoidProfileComponent>(args.User))
            return;

        args.Handled = true;
        var user = args.User;
        var comp = EnsureComp<DisguisedComponent>(user);

        if (comp.Disguised)
        {
            _popup.PopupEntity(Loc.GetString("st-disguise-remote-already-disguised"), user, user);
            return;
        }

        comp.OriginalForm ??= _changelingIdentity.CloneToPausedMap(comp.SelfSettings, user);
        var disguise = _changelingIdentity.CloneToPausedMap(comp.Settings, target);
        if (comp.OriginalForm == null || disguise == null)
        {
            QueueDel(disguise);
            return;
        }

        CopyIdCard(target, disguise.Value, user);
        _mindShield.GetMindshieldStatus(target, out _, out comp.MindShieldVisible);

        QueueDel(comp.DisguiseForm);
        comp.DisguiseForm = disguise;
        _actions.AddAction(user, ref comp.ActionEntity, comp.Action);

        _adminLogger.Add(LogType.Identity, LogImpact.High,
            $"{ToPrettyString(user):player} used {ToPrettyString(ent):entity} to copy the identity of {ToPrettyString(target):player}");

        _popup.PopupEntity(Loc.GetString("st-disguise-remote-copied", ("target", Name(target))), user, user);
        QueueDel(ent);
    }

    private void OnToggleAction(Entity<DisguisedComponent> ent, ref DisguiseToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Toggle(ent);
    }

    private void OnMindShieldStatus(Entity<DisguisedComponent> ent, ref GetMindShieldStatusEvent args)
    {
        if (ent.Comp.Disguised)
            args.IsVisible = ent.Comp.MindShieldVisible;
    }

    private void OnShutdown(Entity<DisguisedComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
        QueueDel(ent.Comp.OriginalForm);
        QueueDel(ent.Comp.DisguiseForm);
    }

    private void Toggle(Entity<DisguisedComponent> ent)
    {
        var (stash, form) = ent.Comp.Disguised
            ? (ent.Comp.DisguiseForm, ent.Comp.OriginalForm)
            : (ent.Comp.OriginalForm, ent.Comp.DisguiseForm);

        if (!Exists(stash) || !Exists(form) || !ProtoMan.Resolve(ent.Comp.Settings, out var settings))
            return;

        MoveEquipment(ent, stash.Value);

        if (TryComp<StorageComponent>(ent, out var storage) && !HasComp<StorageComponent>(form))
            _container.EmptyContainer(storage.Container);

        _visualBody.CopyAppearanceFrom(form.Value, ent.Owner);
        _cloning.CloneComponents(form.Value, ent, settings);

        if (settings.CopyStatusEffects)
            _cloning.CopyStatusEffects(form.Value, ent.Owner, settings.StatusEffectWhitelist, settings.StatusEffectBlacklist);

        _metaData.SetEntityName(ent, Name(form.Value), raiseEvents: false);
        _metaData.SetEntityDescription(ent, Description(form.Value));
        _identity.QueueIdentityUpdate(ent);

        MoveEquipment(form.Value, ent);
        ent.Comp.Disguised = !ent.Comp.Disguised;
        _mindShield.RefreshMindshieldStatus(ent);
    }

    private void MoveEquipment(EntityUid from, EntityUid to)
    {
        var slots = _inventory.GetSlotEnumerator(from);
        while (slots.NextItem(out var item, out var slot))
        {
            if (_inventory.TryGetSlotContainer(to, slot.Name, out var container, out _))
                _container.Insert(item, container, force: true);
        }
    }

    private void CopyIdCard(EntityUid original, EntityUid clone, EntityUid user)
    {
        if (!_idCard.TryFindIdCard(original, out var originalId) || !_idCard.TryFindIdCard(clone, out var cloneId))
            return;

        CopyComp(originalId, cloneId, originalId.Comp);
        if (TryComp<AccessComponent>(originalId, out var access))
            CopyComp(originalId.Owner, cloneId.Owner, access);

        _metaData.SetEntityName(cloneId, Name(originalId), raiseEvents: false);

        if (_inventory.TryGetSlotEntity(clone, "id", out var pda) && TryComp<PdaComponent>(pda, out var pdaComp))
            _pda.SetOwner(pda.Value, pdaComp, user, originalId.Comp.FullName ?? Name(original));
    }
}
