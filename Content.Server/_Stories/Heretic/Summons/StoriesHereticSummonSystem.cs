using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Summons;

public sealed partial class StoriesHereticSummonSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
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

    public bool IsServantOf(EntityUid servant, EntityUid master)
    {
        return TryComp<StoriesHereticSummonComponent>(servant, out var summon) && summon.Master == master;
    }

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        if (args.Effect != StoriesHereticRitualEffect.Summon
            || !ProtoMan.TryIndex(args.Ritual, out var proto)
            || !proto.TryComp<StoriesHereticSummonRitualComponent>(out var ritual, _factory))
            return;

        var mob = Spawn(ritual.Mob, Transform(args.Rune).Coordinates);
        var summon = EnsureComp<StoriesHereticSummonComponent>(mob);
        summon.Master = args.Heretic;
        summon.ExpireTime = _timing.CurTime + summon.ClaimTime;
        foreach (var uid in args.Consumed)
        {
            if (MetaData(uid).EntityPrototype is { } ingredient)
                summon.Ingredients.Add(ingredient.ID);
        }

        _popup.PopupEntity(Loc.GetString("stories-heretic-summon-waiting"), mob, args.Heretic);
        args.Success = true;
    }

    private void OnMindAdded(Entity<StoriesHereticSummonComponent> ent, ref MindAddedMessage args)
    {
        if (ent.Comp.Bound)
            return;

        ent.Comp.Bound = true;
        if (TryComp<StoriesHereticComponent>(ent.Comp.Master, out var heretic))
        {
            _heretic.RegisterSummon((ent.Comp.Master.Value, heretic));
            _popup.PopupEntity(Loc.GetString("stories-heretic-summon-claimed", ("mob", ent.Owner)), ent.Comp.Master.Value, ent.Comp.Master.Value, PopupType.Medium);
        }

        if (TryComp<ActorComponent>(ent, out var actor) && ent.Comp.Master is { } master)
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
        var coords = Transform(ent).Coordinates;
        foreach (var ingredient in ent.Comp.Ingredients)
        {
            Spawn(ingredient, coords);
        }

        ent.Comp.Ingredients.Clear();
        _popup.PopupCoordinates(Loc.GetString(message, ("mob", ent.Owner)), coords, PopupType.MediumCaution);
        QueueDel(ent);
    }
}
