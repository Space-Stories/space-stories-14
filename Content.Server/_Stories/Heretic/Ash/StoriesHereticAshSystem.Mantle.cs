using Content.Server.Administration.Logs;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Body;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server._Stories.Heretic.Ash;

public sealed partial class StoriesHereticAshSystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DetachableOrganSystem _detachable = default!;

    private void InitializeMantle()
    {
        SubscribeLocalEvent<StoriesHereticScorchedMantleComponent, GotEquippedEvent>(OnMantleEquipped);
        SubscribeLocalEvent<StoriesHereticScorchedMantleComponent, GotUnequippedEvent>(OnMantleUnequipped);
        SubscribeLocalEvent<StoriesHereticScorchedMantleComponent, ComponentShutdown>(OnMantleShutdown);
        SubscribeLocalEvent<StoriesHereticScorchedMantleComponent, StoriesHereticMantleFlamesEvent>(OnMantleFlames);
    }

    private void UpdateMantles(TimeSpan now)
    {
        var mantles = EntityQueryEnumerator<StoriesHereticScorchedMantleComponent>();
        while (mantles.MoveNext(out var uid, out var mantle))
        {
            if (mantle.Wearer is not { } wearer || now < mantle.NextPulse)
                continue;

            if (!HasComp<StoriesHereticComponent>(wearer))
            {
                mantle.NextPulse = now + mantle.BurnInterval;
                BurnLimb((uid, mantle), wearer);
                continue;
            }

            if (!mantle.Flames)
                continue;

            mantle.NextPulse = now + mantle.FlameInterval;
            Ignite(wearer, mantle.FlameStacks);
        }
    }

    private void OnMantleEquipped(Entity<StoriesHereticScorchedMantleComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.OUTERCLOTHING) == 0)
            return;

        var wearer = args.EquipTarget;
        ent.Comp.Wearer = wearer;

        if (!TryComp<StoriesHereticComponent>(wearer, out var heretic))
        {
            ent.Comp.NextPulse = _timing.CurTime + ent.Comp.BurnInterval;
            _popup.PopupEntity(Loc.GetString("stories-heretic-mantle-burns"), wearer, wearer);
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(wearer):user} put on {ToPrettyString(ent):item} without being a heretic and starts losing limbs");
            return;
        }

        _heretic.SetAuraHidden((wearer, heretic), true);

        if (TryComp<FlammableComponent>(wearer, out var flammable))
        {
            ent.Comp.SavedFirestackFade = flammable.FirestackFade;
            flammable.FirestackFade = 0f;
            Dirty(wearer, flammable);
        }
    }

    private void OnMantleUnequipped(Entity<StoriesHereticScorchedMantleComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.Wearer == args.EquipTarget)
            ReleaseWearer(ent);
    }

    private void OnMantleShutdown(Entity<StoriesHereticScorchedMantleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Wearer is { } wearer && !TerminatingOrDeleted(wearer))
            ReleaseWearer(ent);
    }

    private void ReleaseWearer(Entity<StoriesHereticScorchedMantleComponent> ent)
    {
        if (ent.Comp.Wearer is not { } wearer)
            return;

        if (ent.Comp.Flames)
            SetMantleFlames(ent, wearer, false);

        if (TryComp<StoriesHereticComponent>(wearer, out var heretic))
            _heretic.SetAuraHidden((wearer, heretic), false);

        if (ent.Comp.SavedFirestackFade is { } fade && TryComp<FlammableComponent>(wearer, out var flammable))
        {
            flammable.FirestackFade = fade;
            Dirty(wearer, flammable);
        }

        ent.Comp.SavedFirestackFade = null;
        ent.Comp.Wearer = null;
    }

    private void OnMantleFlames(Entity<StoriesHereticScorchedMantleComponent> ent, ref StoriesHereticMantleFlamesEvent args)
    {
        if (args.Handled || ent.Comp.Wearer != args.Performer || !HasComp<StoriesHereticComponent>(args.Performer))
            return;

        args.Handled = true;
        SetMantleFlames(ent, args.Performer, !ent.Comp.Flames);
    }

    private void SetMantleFlames(Entity<StoriesHereticScorchedMantleComponent> ent, EntityUid wearer, bool enabled)
    {
        ent.Comp.Flames = enabled;
        if (TryComp<ActionGrantComponent>(ent, out var grant))
        {
            foreach (var action in grant.ActionEntities)
            {
                _actions.SetToggled(action, enabled);
            }
        }

        _popup.PopupEntity(Loc.GetString(enabled ? "stories-heretic-mantle-flames-on" : "stories-heretic-mantle-flames-off"),
            wearer,
            wearer);

        if (!enabled)
        {
            _flammable.TryExtinguish(wearer);
            return;
        }

        ent.Comp.NextPulse = _timing.CurTime + ent.Comp.FlameInterval;
        Ignite(wearer, ent.Comp.FlameStacks);
    }

    private void BurnLimb(Entity<StoriesHereticScorchedMantleComponent> ent, EntityUid wearer)
    {
        foreach (var organ in _body.EnumerateOrgans<DetachableOrganComponent>(wearer))
        {
            if (organ.Comp1.Category is not { } category || !ent.Comp.BurnLimbs.Contains(category))
                continue;

            var coords = Transform(wearer).Coordinates;
            if (_detachable.Detach((organ.Owner, organ.Comp2)) is { } detached)
                QueueDel(detached);

            Spawn(ent.Comp.LimbAsh, coords);
            _popup.PopupEntity(Loc.GetString("stories-heretic-mantle-limb-burnt"), wearer, wearer);
            _adminLog.Add(LogType.Damaged, LogImpact.Medium,
                $"{ToPrettyString(ent):item} burnt the {category} of {ToPrettyString(wearer):user}");
            return;
        }
    }
}
