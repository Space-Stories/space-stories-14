using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server._Stories.Heretic.Flesh;

// Извивающиеся объятия — мантия пути Плоти.
public sealed partial class StoriesHereticFleshSystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    private void InitializeEmbrace()
    {
        SubscribeLocalEvent<StoriesHereticWrithingEmbraceComponent, GotEquippedEvent>(OnEmbraceEquipped);
        SubscribeLocalEvent<StoriesHereticWrithingEmbraceComponent, GotUnequippedEvent>(OnEmbraceUnequipped);
        SubscribeLocalEvent<StoriesHereticWrithingEmbraceComponent, ComponentShutdown>(OnEmbraceShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var embraces = EntityQueryEnumerator<StoriesHereticWrithingEmbraceComponent>();
        while (embraces.MoveNext(out var uid, out var embrace))
        {
            if (embrace.Wearer is not { } wearer || now < embrace.NextPulse)
                continue;

            embrace.NextPulse = now + embrace.Interval;
            if (HasComp<StoriesHereticComponent>(wearer))
                HealServants((uid, embrace), wearer);
            else if (embrace.WoundsLeft > 0)
                Wound((uid, embrace), wearer);
        }
    }

    private void OnEmbraceEquipped(Entity<StoriesHereticWrithingEmbraceComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.OUTERCLOTHING) == 0)
            return;

        var wearer = args.EquipTarget;
        ent.Comp.Wearer = wearer;
        ent.Comp.NextPulse = _timing.CurTime + ent.Comp.Interval;

        if (!TryComp<StoriesHereticComponent>(wearer, out var heretic))
        {
            // Как robes_side_effect в tg: чужаку мантия по очереди рвёт каждую часть тела.
            ent.Comp.WoundsLeft = ent.Comp.Wounds;
            _popup.PopupEntity(Loc.GetString("stories-heretic-embrace-wounds"), wearer, wearer);
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(wearer):user} put on {ToPrettyString(ent):item} without being a heretic and starts getting wounded");
            return;
        }

        _heretic.SetAuraHidden((wearer, heretic), true);
    }

    private void OnEmbraceUnequipped(Entity<StoriesHereticWrithingEmbraceComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.Wearer == args.EquipTarget)
            ReleaseEmbrace(ent);
    }

    /// <summary>
    /// Страховка: мантия удалена или лишилась компонента, не пройдя через снятие.
    /// </summary>
    private void OnEmbraceShutdown(Entity<StoriesHereticWrithingEmbraceComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Wearer is { } wearer && !TerminatingOrDeleted(wearer))
            ReleaseEmbrace(ent);
    }

    private void ReleaseEmbrace(Entity<StoriesHereticWrithingEmbraceComponent> ent)
    {
        if (ent.Comp.Wearer is not { } wearer)
            return;

        if (TryComp<StoriesHereticComponent>(wearer, out var heretic))
            _heretic.SetAuraHidden((wearer, heretic), false);

        ent.Comp.Wearer = null;
        ent.Comp.WoundsLeft = 0;
    }

    /// <summary>
    /// Аура aura_healing из tg: лечит живых слуг носителя в радиусе, самого носителя — нет.
    /// </summary>
    private void HealServants(Entity<StoriesHereticWrithingEmbraceComponent> ent, EntityUid wearer)
    {
        var center = _transform.GetMapCoordinates(wearer);
        var servants = EntityQueryEnumerator<StoriesHereticServantComponent, TransformComponent>();
        while (servants.MoveNext(out var uid, out var servant, out var xform))
        {
            if (servant.Master != wearer || !_mobState.IsAlive(uid))
                continue;

            var pos = _transform.GetMapCoordinates(uid, xform);
            if (pos.MapId != center.MapId || !pos.InRange(center, ent.Comp.Range))
                continue;

            foreach (var (group, amount) in ent.Comp.Heal.Damage)
            {
                _damageable.HealDistributed(uid, amount, group, wearer);
            }
        }
    }

    private void Wound(Entity<StoriesHereticWrithingEmbraceComponent> ent, EntityUid wearer)
    {
        ent.Comp.WoundsLeft--;
        // Рана рвёт изнутри: броня самой мантии её не смягчает.
        _damageable.TryChangeDamage(wearer, ent.Comp.Wound, ignoreResistances: true, origin: ent);
        _bloodstream.TryModifyBleedAmount(wearer, ent.Comp.WoundBleed);
        _popup.PopupEntity(Loc.GetString("stories-heretic-embrace-wound"), wearer, wearer);
    }
}
