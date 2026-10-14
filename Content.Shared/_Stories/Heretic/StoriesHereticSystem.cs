using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Stories.Heretic;

/// <summary>
/// Единственный владелец <see cref="StoriesHereticComponent"/>. Остальные системы еретика используют его API.
/// </summary>
public sealed partial class StoriesHereticSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private const string KnowledgeBuiType = "StoriesHereticKnowledgeBoundUserInterface";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StoriesHereticComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<StoriesHereticComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StoriesHereticComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticOpenKnowledgeEvent>(OnOpenKnowledge);

        InitializeKnowledge();
        InitializeFocus();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextPassivePoint)
                continue;

            comp.NextPassivePoint = now + comp.PassivePointInterval;
            AddPoints((uid, comp), 1);
        }
    }

    private void OnMapInit(Entity<StoriesHereticComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextPassivePoint = _timing.CurTime + ent.Comp.PassivePointInterval;

        if (_actions.AddAction(ent, ent.Comp.KnowledgeAction) is { } knowledgeAction)
            ent.Comp.GrantedActions.Add(knowledgeAction);

        // Без проверки взаимодействия: знания изучаются и в наручниках, как в tg. Чужие сообщения отсекает OnLearnMessage.
        EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi(ent.Owner, StoriesHereticKnowledgeUiKey.Key, new InterfaceData(KnowledgeBuiType, requireInputValidation: false));

        foreach (var (id, knowledge) in AllKnowledge)
        {
            if (knowledge.Starting)
                Grant(ent, id, knowledge);
        }

        PickKnowledgeRitualTags(ent);
        Dirty(ent);
    }

    /// <summary>
    /// Данные еретика видит только он сам: иначе по сети можно вычислить антагониста.
    /// </summary>
    private void OnGetStateAttempt(Entity<StoriesHereticComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Player?.AttachedEntity != ent.Owner)
            args.Cancelled = true;
    }

    private void OnOpenKnowledge(Entity<StoriesHereticComponent> ent, ref StoriesHereticOpenKnowledgeEvent args)
    {
        if (args.Handled)
            return;

        _ui.TryToggleUi(ent.Owner, StoriesHereticKnowledgeUiKey.Key, ent.Owner);
        args.Handled = true;
    }

    private void PickKnowledgeRitualTags(Entity<StoriesHereticComponent> ent)
    {
        if (_net.IsClient || !ProtoMan.TryIndex(ent.Comp.KnowledgeRitualDataset, out var dataset))
            return;

        var pool = new List<string>(dataset.Values);
        _random.Shuffle(pool);

        ent.Comp.KnowledgeRitualTags.Clear();
        for (var i = 0; i < Math.Min(ent.Comp.KnowledgeRitualTagCount, pool.Count); i++)
        {
            ent.Comp.KnowledgeRitualTags.Add(pool[i]);
        }
    }

    public void AddPoints(Entity<StoriesHereticComponent> ent, int amount)
    {
        ent.Comp.Points += amount;
        ent.Comp.PointsGained += Math.Max(0, amount);
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("stories-heretic-points-gained", ("amount", amount)), ent, ent);
        UpdateAura(ent);
    }

    /// <summary>
    /// Как should_show_aura в tg: аура видна на поздней стадии пути, но не после Пира сов.
    /// </summary>
    public bool ShouldShowAura(StoriesHereticComponent comp)
    {
        return comp.PointsGained > comp.PointsToAura && comp.CanAscend;
    }

    /// <summary>
    /// Создаёт или убирает ауру по <see cref="ShouldShowAura"/>. Повторный вызов не дублирует ауру.
    /// </summary>
    private void UpdateAura(Entity<StoriesHereticComponent> ent)
    {
        if (_net.IsClient)
            return;

        var exists = !TerminatingOrDeleted(ent.Comp.AuraEntity);
        if (ShouldShowAura(ent.Comp) == exists)
            return;

        if (exists)
        {
            QueueDel(ent.Comp.AuraEntity);
            ent.Comp.AuraEntity = null;
            return;
        }

        ent.Comp.AuraEntity = SpawnAttachedTo(ent.Comp.Aura, new EntityCoordinates(ent, default));
    }

    private void OnRemove(Entity<StoriesHereticComponent> ent, ref ComponentRemove args)
    {
        if (_net.IsServer)
            QueueDel(ent.Comp.AuraEntity);

        // Повторная выдача компонента (админ, ForceMakeAntag) создаёт новый — старые действия не должны остаться дублями.
        if (TerminatingOrDeleted(ent))
            return;

        foreach (var action in ent.Comp.GrantedActions)
        {
            _actions.RemoveAction(ent.Owner, action);
            // RemoveAction лишь отвязывает действие от владельца, сущность остаётся в контейнере.
            PredictedQueueDel(action);
        }
    }

    private void OnExamined(Entity<StoriesHereticComponent> ent, ref ExaminedEvent args)
    {
        if (!ShouldShowAura(ent.Comp))
            return;

        args.PushMarkup(Loc.GetString("stories-heretic-aura-examine", ("ent", ent.Owner)));
        if (ObjectivesComplete(ent.Comp))
            args.PushMarkup(Loc.GetString("stories-heretic-aura-examine-ascend", ("ent", ent.Owner)));
    }

    public bool ObjectivesComplete(StoriesHereticComponent comp)
    {
        return comp.LearnedCount >= comp.RequiredKnowledge
               && comp.SacrificeCount >= comp.RequiredSacrifices
               && comp.HeadSacrificeCount >= comp.RequiredHeadSacrifices
               && comp.SummonCount >= comp.RequiredSummons;
    }

    public bool IsTarget(Entity<StoriesHereticComponent> ent, EntityUid uid)
    {
        return ent.Comp.Targets.Contains(GetNetEntity(uid));
    }

    public void SetTargets(Entity<StoriesHereticComponent> ent, List<EntityUid> targets)
    {
        ent.Comp.Targets.Clear();
        foreach (var target in targets)
        {
            ent.Comp.Targets.Add(GetNetEntity(target));
        }

        ent.Comp.TrackedTarget = 0;
        Dirty(ent);
    }

    /// <summary>
    /// Переключает Живое сердце на следующую существующую цель.
    /// </summary>
    public EntityUid? CycleTrackedTarget(Entity<StoriesHereticComponent> ent)
    {
        for (var i = 0; i < ent.Comp.Targets.Count; i++)
        {
            ent.Comp.TrackedTarget = (ent.Comp.TrackedTarget + 1) % ent.Comp.Targets.Count;
            // На paused-карте (крио) цель недостижима — сердце её пропускает.
            if (TryGetEntity(ent.Comp.Targets[ent.Comp.TrackedTarget], out var target)
                && !TerminatingOrDeleted(target)
                && !Paused(target.Value))
            {
                Dirty(ent);
                return target;
            }
        }

        return null;
    }

    public void RemoveTarget(Entity<StoriesHereticComponent> ent, EntityUid target)
    {
        if (ent.Comp.Targets.Remove(GetNetEntity(target)))
            Dirty(ent);
    }

    public void SetRequiredHeadSacrifices(Entity<StoriesHereticComponent> ent, int required)
    {
        ent.Comp.RequiredHeadSacrifices = required;
        Dirty(ent);
    }

    /// <summary>
    /// Засчитывает призыв, занятый игроком. Каждый игрок считается один раз: иначе смерть и повторный призыв накручивали бы цель.
    /// </summary>
    public bool RegisterSummon(Entity<StoriesHereticComponent> ent, NetUserId player)
    {
        if (!ent.Comp.SummonedPlayers.Add(player))
            return false;

        ent.Comp.SummonCount++;
        Dirty(ent);
        return true;
    }

    public void MakeServant(EntityUid uid, EntityUid master)
    {
        var servant = EnsureComp<StoriesHereticServantComponent>(uid);
        servant.Master = master;
        Dirty(uid, servant);
    }

    /// <summary>
    /// Слуга (призыв или гуль) любого еретика или, если указан <paramref name="master"/>, именно этого.
    /// </summary>
    public bool IsServant(EntityUid uid, EntityUid? master = null)
    {
        return TryComp<StoriesHereticServantComponent>(uid, out var servant) && (master == null || servant.Master == master);
    }

    public void RegisterSacrifice(Entity<StoriesHereticComponent> ent, EntityUid victim, bool head)
    {
        ent.Comp.Targets.Remove(GetNetEntity(victim));
        ent.Comp.SacrificeCount++;
        if (head)
            ent.Comp.HeadSacrificeCount++;

        Dirty(ent);
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(ent):user} sacrificed {ToPrettyString(victim):target} (head: {head})");
        AddPoints(ent, head ? ent.Comp.HeadSacrificePoints : ent.Comp.SacrificePoints);
    }

    public void RemoveRitual(Entity<StoriesHereticComponent> ent, EntProtoId ritual)
    {
        if (ent.Comp.Rituals.Remove(ritual))
            Dirty(ent);
    }

    /// <summary>
    /// Снимает с тела всю одежду и роняет вещи из рук: трупы для Вознесения поглощаются без вещей.
    /// </summary>
    public void DropEverything(EntityUid uid)
    {
        if (_inventory.TryGetContainerSlotEnumerator(uid, out var slots))
        {
            while (slots.NextItem(out _, out var slot))
            {
                _inventory.TryUnequip(uid, slot.Name, true, true);
            }
        }

        _hands.DropAll(uid, checkActionBlocker: false, doDropInteraction: false);
    }

    public void SetAscended(Entity<StoriesHereticComponent> ent)
    {
        ent.Comp.Ascended = true;
        Dirty(ent);
        _adminLog.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(ent):user} ascended as a heretic");
    }

    public void MarkKnowledgeRitualUsed(Entity<StoriesHereticComponent> ent)
    {
        ent.Comp.KnowledgeRitualUsed = true;
        Dirty(ent);
    }

    public void TrackLimitedResult(Entity<StoriesHereticComponent> ent, EntProtoId ritual, EntityUid result)
    {
        if (!ent.Comp.LimitedResults.TryGetValue(ritual, out var list))
        {
            list = new List<EntityUid>();
            ent.Comp.LimitedResults[ritual] = list;
        }

        list.Add(result);
    }

    public int CountLimitedResults(Entity<StoriesHereticComponent> ent, EntProtoId ritual)
    {
        if (!ent.Comp.LimitedResults.TryGetValue(ritual, out var list))
            return 0;

        list.RemoveAll(e => TerminatingOrDeleted(e));
        return list.Count;
    }
}
