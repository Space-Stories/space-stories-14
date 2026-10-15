using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Fluids.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticRitualSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private static readonly ProtoId<TagPrototype> WriteTag = "Write";
    private static readonly SoundSpecifier RitualSound = new SoundPathSpecifier("/Audio/Magic/staff_animation.ogg");

    // Отделённая часть тела засчитывается только как конечность, а не как всё, что в ней лежит (глаза в голове).
    private static readonly HashSet<ProtoId<OrganCategoryPrototype>> LimbCategories =
    [
        "Head", "ArmLeft", "ArmRight", "HandLeft", "HandRight", "LegLeft", "LegRight", "FootLeft", "FootRight",
    ];

    private readonly HashSet<EntityUid> _nearby = new();
    private readonly HashSet<EntityUid> _used = new();
    private readonly List<EntityUid> _consumed = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticDrawRuneEvent>(OnDrawRune);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticDrawRuneDoAfterEvent>(OnDrawRuneDoAfter);
        SubscribeLocalEvent<StoriesHereticRuneComponent, InteractHandEvent>(OnRuneInteract);
    }

    private void OnDrawRune(Entity<StoriesHereticComponent> ent, ref StoriesHereticDrawRuneEvent args)
    {
        if (args.Handled)
            return;

        if (_hands.GetActiveItem(ent.Owner) is not { } pen || !_tag.HasTag(pen, WriteTag))
        {
            _popup.PopupClient(Loc.GetString("stories-heretic-rune-need-pen"), ent, ent);
            return;
        }

        if (_turf.GetTileRef(args.Target) is not { } tile || _turf.IsSpace(tile))
        {
            _popup.PopupClient(Loc.GetString("stories-heretic-rune-bad-tile"), ent, ent);
            return;
        }

        _nearby.Clear();
        _lookup.GetEntitiesInRange(args.Target, 2f, _nearby);
        foreach (var uid in _nearby)
        {
            if (!HasComp<StoriesHereticRuneComponent>(uid))
                continue;

            _popup.PopupClient(Loc.GetString("stories-heretic-rune-too-close"), ent, ent);
            return;
        }

        var duration = _heretic.HasOpenCodex(ent)
            ? args.Duration / args.CodexSpeedMultiplier
            : args.Duration;

        var doAfter = new DoAfterArgs(EntityManager,
            ent,
            duration,
            new StoriesHereticDrawRuneDoAfterEvent(GetNetCoordinates(args.Target), args.Rune),
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDrawRuneDoAfter(Entity<StoriesHereticComponent> ent, ref StoriesHereticDrawRuneDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        Spawn(args.Rune, GetCoordinates(args.Coordinates));
    }

    private void OnRuneInteract(Entity<StoriesHereticRuneComponent> rune, ref InteractHandEvent args)
    {
        if (args.Handled || !TryComp<StoriesHereticComponent>(args.User, out var heretic))
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        TryPerform((args.User, heretic), rune);
    }

    /// <summary>
    /// Выполняет первый подходящий известный ритуал на руне. Только сервер.
    /// </summary>
    public bool TryPerform(Entity<StoriesHereticComponent> heretic, EntityUid rune)
    {
        var radius = CompOrNull<StoriesHereticRuneComponent>(rune)?.Radius ?? 1.5f;
        var coords = Transform(rune).Coordinates;

        _nearby.Clear();
        _lookup.GetEntitiesInRange(coords, radius, _nearby, LookupFlags.Uncontained);
        _nearby.Remove(heretic);
        _nearby.Remove(rune);
        // Поглощённое прошлым ритуалом удаляется только в конце тика — второй ритуал в том же тике его не видит.
        _nearby.RemoveWhere(uid => TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid));

        // Подсказка берётся от ритуала, к которому игрок подошёл ближе всего:
        // очки = число выполненных условий, при равенстве — первый.
        string? failReason = null;
        var bestScore = 0;
        foreach (var ritualId in new List<EntProtoId>(heretic.Comp.Rituals))
        {
            if (!ProtoMan.TryIndex(ritualId, out var proto)
                || !proto.TryComp<StoriesHereticRitualComponent>(out var ritual, _factory))
                continue;

            if (!TryMatch(ritual.Ingredients, out var missing, out var matched))
            {
                RecordFailure(missing, matched);
                continue;
            }

            if (ritual.Limit is { } limit
                && !(ritual.AscendedUnlimited && heretic.Comp.Ascended)
                && _heretic.CountLimitedResults(heretic, ritualId) >= limit)
            {
                RecordFailure(Loc.GetString("stories-heretic-ritual-limit"), matched + 1);
                continue;
            }

            if (!TryRunEffect(heretic, rune, ritualId, ritual.Effect, out var effectFail, out var effectScore))
            {
                RecordFailure(effectFail, matched + effectScore);
                continue;
            }

            // Из стопки ингредиент — одна единица, остальное остаётся на руне.
            foreach (var uid in _consumed)
            {
                if (TryComp<StackComponent>(uid, out var stack) && stack.Count > 1)
                    _stack.ReduceCount((uid, stack), 1);
                else
                    QueueDel(uid);
            }

            foreach (var result in ritual.Results)
            {
                var spawned = Spawn(result, coords);
                if (ritual.Limit != null)
                    _heretic.TrackLimitedResult(heretic, ritualId, spawned);
            }

            _audio.PlayPvs(RitualSound, rune);
            _popup.PopupEntity(Loc.GetString("stories-heretic-ritual-success"), rune, heretic);
            return true;
        }

        _popup.PopupEntity(failReason ?? Loc.GetString("stories-heretic-ritual-nothing"), rune, heretic);
        return false;

        void RecordFailure(string? reason, int score)
        {
            if (reason == null || score <= bestScore)
                return;

            failReason = reason;
            bestScore = score;
        }
    }

    /// <summary>
    /// <paramref name="score"/> — сколько условий эффекта выполнено (для выбора подсказки при неудаче).
    /// </summary>
    private bool TryRunEffect(Entity<StoriesHereticComponent> heretic,
        EntityUid rune,
        EntProtoId ritualId,
        StoriesHereticRitualEffect effect,
        out string? failReason,
        out int score)
    {
        failReason = null;
        score = 0;

        switch (effect)
        {
            case StoriesHereticRitualEffect.None:
                return true;

            case StoriesHereticRitualEffect.Knowledge:
                if (heretic.Comp.KnowledgeRitualUsed)
                    return false;

                ProtoId<TagPrototype>? missingTag = null;
                foreach (var tag in heretic.Comp.KnowledgeRitualTags)
                {
                    if (TryUseTagged(tag))
                        score++;
                    else
                        missingTag ??= tag;
                }

                if (missingTag is { } missing)
                {
                    failReason = Loc.GetString("stories-heretic-ritual-missing",
                        ("item", Loc.GetString($"stories-heretic-tag-{missing.Id}")),
                        ("amount", 1));
                    return false;
                }

                _heretic.MarkKnowledgeRitualUsed(heretic);
                _heretic.RemoveRitual(heretic, ritualId);
                _heretic.AddPoints(heretic, heretic.Comp.KnowledgeRitualPoints);
                return true;

            default:
                var ev = new StoriesHereticRitualEffectEvent(heretic, rune, ritualId, effect, _nearby, _consumed);
                RaiseLocalEvent(ref ev);
                if (ev.Success)
                    return true;

                // Обработчик ставит FailReason только если нашёл, с чем работать (тело на руне и т.п.).
                failReason = ev.FailReason;
                score = 1;
                return false;
        }
    }

    private bool TryUseTagged(ProtoId<TagPrototype> tag)
    {
        foreach (var uid in _nearby)
        {
            if (_used.Contains(uid) || !_tag.HasTag(uid, tag))
                continue;

            _used.Add(uid);
            _consumed.Add(uid);
            return true;
        }

        return false;
    }

    /// <summary>
    /// <paramref name="matched"/> — число полностью найденных ингредиентов.
    /// </summary>
    private bool TryMatch(List<StoriesHereticIngredient> ingredients, out string? missing, out int matched)
    {
        _used.Clear();
        _consumed.Clear();
        missing = null;
        matched = 0;

        foreach (var ingredient in ingredients)
        {
            var found = 0;
            foreach (var uid in _nearby)
            {
                if (found >= ingredient.Amount)
                    break;

                if (_used.Contains(uid) || !Matches(ingredient, uid))
                    continue;

                _used.Add(uid);
                found++;
                if (ingredient.Consume)
                    _consumed.Add(uid);
            }

            if (found >= ingredient.Amount)
            {
                matched++;
                continue;
            }

            missing ??= Loc.GetString("stories-heretic-ritual-missing",
                ("item", Loc.GetString(ingredient.Name)),
                ("amount", ingredient.Amount - found));
        }

        return missing == null;
    }

    public bool Matches(StoriesHereticIngredient ingredient, EntityUid uid)
    {
        if (ingredient.Blacklist != null && _whitelist.IsValid(ingredient.Blacklist, uid))
            return false;

        if (ingredient.Whitelist != null && _whitelist.IsValid(ingredient.Whitelist, uid))
            return true;

        if (ingredient.Prototypes != null
            && MetaData(uid).EntityPrototype is { } proto
            && ingredient.Prototypes.Contains(proto.ID))
            return true;

        if (ingredient.OrganCategory != null
            && TryComp<OrganComponent>(uid, out var organ)
            && organ.Category == ingredient.OrganCategory)
            return true;

        // Отделённая часть тела (DetachedBody) — сущность без MobState, орган лежит в её контейнере.
        if (ingredient.OrganCategory != null
            && LimbCategories.Contains(ingredient.OrganCategory.Value)
            && !HasComp<MobStateComponent>(uid)
            && _containers.TryGetContainer(uid, BodyComponent.ContainerID, out var bodyOrgans))
        {
            foreach (var contained in bodyOrgans.ContainedEntities)
            {
                if (TryComp<OrganComponent>(contained, out var inner) && inner.Category == ingredient.OrganCategory)
                    return true;
            }
        }

        if (ingredient.Reagents != null
            && TryComp<PuddleComponent>(uid, out var puddle)
            && _solutions.TryGetSolution(uid, puddle.SolutionName, out _, out var solution))
        {
            var amount = FixedPoint2.Zero;
            foreach (var reagent in ingredient.Reagents)
            {
                amount += solution.GetTotalPrototypeQuantity(reagent);
            }

            if (amount >= ingredient.ReagentAmount)
                return true;
        }

        if (ingredient.ClothingSlots is { } slots
            && TryComp<ClothingComponent>(uid, out var clothing)
            && (clothing.Slots & slots) != 0)
            return true;

        return false;
    }
}
