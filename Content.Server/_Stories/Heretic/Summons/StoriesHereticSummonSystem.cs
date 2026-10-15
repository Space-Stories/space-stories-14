using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Shared.Fluids.Components;
using Content.Shared.Stacks;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Summons;

public sealed partial class StoriesHereticSummonSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private OrganRelationSystem _relations = default!;
    [Dependency] private StackSystem _stack = default!;

    private static readonly EntProtoId ServeMaster = "STHereticObjectiveServeMaster";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticSummonComponent, MapInitEvent>(OnSummonMapInit);
        SubscribeLocalEvent<StoriesHereticSummonComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<StoriesHereticSummonComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticSummonComponent>();
        while (query.MoveNext(out var uid, out var summon))
        {
            if (!summon.Bound && now >= summon.ExpireTime)
                Collapse((uid, summon), "stories-heretic-summon-unclaimed");
        }
    }

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        if (args.Effect != StoriesHereticRitualEffect.Summon
            || !ProtoMan.TryIndex(args.Ritual, out var proto)
            || !proto.TryComp<StoriesHereticSummonRitualComponent>(out var ritual, _factory))
            return;

        var mob = Spawn(ritual.Mob, Transform(args.Rune).Coordinates);
        var summon = EnsureComp<StoriesHereticSummonComponent>(mob);
        EnsureComp<StoriesHereticSummonWaitingComponent>(mob);
        summon.Master = args.Heretic;
        summon.ExpireTime = _timing.CurTime + summon.ClaimTime;
        _heretic.MakeServant(mob, args.Heretic);
        foreach (var uid in args.Consumed)
        {
            if (HasComp<PuddleComponent>(uid))
                continue;

            if (TryRecordLimb(uid, summon))
                continue;

            if (TryComp<StackComponent>(uid, out var stack))
                summon.Stacks.Add(stack.StackTypeId);
            else if (MetaData(uid).EntityPrototype is { } ingredient)
                summon.Ingredients.Add(ingredient.ID);
        }

        _popup.PopupEntity(Loc.GetString("stories-heretic-summon-waiting"), mob, args.Heretic);
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Heretic):user} summoned {ToPrettyString(mob):target}");
        args.Success = true;
    }

    /// <summary>Отделённая часть тела (тело без MobState): запоминаем органы внутри.</summary>
    private bool TryRecordLimb(EntityUid uid, StoriesHereticSummonComponent summon)
    {
        if (!HasComp<BodyComponent>(uid) || HasComp<MobStateComponent>(uid)
            || MetaData(uid).EntityPrototype is not { } body
            || !_containers.TryGetContainer(uid, BodyComponent.ContainerID, out var container))
            return false;

        var limb = new StoriesHereticSummonLimb { Body = body.ID };
        var organs = container.ContainedEntities;
        foreach (var organ in organs)
        {
            if (MetaData(organ).EntityPrototype is not { } proto)
                continue;

            var parent = -1;
            if (TryComp<ChildOrganComponent>(organ, out var child) && child.Parent is { } parentUid)
            {
                for (var i = 0; i < limb.Organs.Count; i++)
                {
                    if (organs[i] == parentUid)
                        parent = i;
                }
            }

            limb.Organs.Add(proto.ID);
            limb.Parents.Add(parent);
        }

        summon.Limbs.Add(limb);
        return true;
    }

    private void SpawnLimb(StoriesHereticSummonLimb limb, EntityCoordinates coords)
    {
        var body = Spawn(limb.Body, coords);
        if (!_containers.TryGetContainer(body, BodyComponent.ContainerID, out var container))
            return;

        var spawned = new List<EntityUid>();
        foreach (var organ in limb.Organs)
        {
            var uid = Spawn(organ, coords);
            _containers.Insert(uid, container, force: true);
            spawned.Add(uid);
        }

        for (var i = 0; i < spawned.Count; i++)
        {
            if (limb.Parents[i] is >= 0 and var parent && parent < spawned.Count)
                _relations.Relate(spawned[parent], spawned[i]);
        }
    }

    // Моб, созданный не ритуалом (админ-спавн), тоже получает срок ожидания игрока.
    private void OnSummonMapInit(Entity<StoriesHereticSummonComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ExpireTime = _timing.CurTime + ent.Comp.ClaimTime;
    }

    public EntityUid? GetMaster(EntityUid summon) => CompOrNull<StoriesHereticSummonComponent>(summon)?.Master;

    private void OnMindAdded(Entity<StoriesHereticSummonComponent> ent, ref MindAddedMessage args)
    {
        if (ent.Comp.Bound)
            return;

        ent.Comp.Bound = true;
        RemComp<StoriesHereticSummonWaitingComponent>(ent);
        // Разум, уже служивший (перенос между телами), второй такой цели не получает.
        if (!_mind.TryFindObjective(args.Mind.AsNullable(), ServeMaster, out _)
            && !_mind.TryAddObjective(args.Mind.Owner, args.Mind.Comp, ServeMaster))
            Log.Error($"Failed to add objective {ServeMaster} to summon {ToPrettyString(ent)}");
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(ent):player} took the summon role of {ToPrettyString(ent.Comp.Master):user}");
        if (TryComp<StoriesHereticComponent>(ent.Comp.Master, out var heretic))
        {
            if (args.Mind.Comp.UserId is { } player)
                _heretic.RegisterSummon((ent.Comp.Master.Value, heretic), player);
            _popup.PopupEntity(Loc.GetString("stories-heretic-summon-claimed", ("mob", ent.Owner)), ent.Comp.Master.Value, ent.Comp.Master.Value, PopupType.Medium);
        }

        // Удалённого хозяина в брифинге не назвать: призыв всё равно привязан, но без брифинга.
        if (TryComp<ActorComponent>(ent, out var actor) && ent.Comp.Master is { } master && !TerminatingOrDeleted(master))
            _chat.DispatchServerMessage(actor.PlayerSession, Loc.GetString("stories-heretic-summon-briefing", ("master", master)));
    }

    private void OnMobStateChanged(Entity<StoriesHereticSummonComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            Collapse(ent, "stories-heretic-summon-collapse");
    }

    /// <summary>
    /// Моб растворяется и оставляет ингредиенты рецепта: ритуал не пропадает впустую.
    /// </summary>
    private void Collapse(Entity<StoriesHereticSummonComponent> ent, LocId message)
    {
        // Уже схлопнулся (смерть и таймер в один тик). Очередь удаления не проверяем: моба могли разгибать до обработчика.
        if (ent.Comp.Collapsed || TerminatingOrDeleted(ent))
            return;

        ent.Comp.Collapsed = true;

        var coords = Transform(ent).Coordinates;
        foreach (var stack in ent.Comp.Stacks)
        {
            _stack.SpawnAtPosition(1, stack, coords);
        }

        ent.Comp.Stacks.Clear();
        foreach (var ingredient in ent.Comp.Ingredients)
        {
            Spawn(ingredient, coords);
        }

        ent.Comp.Ingredients.Clear();
        foreach (var limb in ent.Comp.Limbs)
        {
            SpawnLimb(limb, coords);
        }

        ent.Comp.Limbs.Clear();
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"Summon {ToPrettyString(ent):target} of {ToPrettyString(ent.Comp.Master):user} collapsed ({message.Id})");
        _popup.PopupCoordinates(Loc.GetString(message, ("mob", ent.Owner)), coords, PopupType.MediumCaution);
        QueueDel(ent);
    }
}
