using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Dataset;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Organs;

public sealed partial class StoriesHereticCorruptedOrganSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;

    private static readonly EntProtoId BlastEffect = "STHereticCurseBlast";
    private static readonly EntProtoId Blindness = "StatusEffectBlindness";
    private static readonly ProtoId<DatasetPrototype> Shadowspeak = "STHereticShadowspeak";

    /// <summary>
    /// Реагенты, которые испорченная печень подмешивает в выпитое. SpaceMirage в проекте нет — отброшен.
    /// </summary>
    private static readonly string[] LiverReagents = ["Rum", "Vodka", "Happiness", "MindbreakerToxin", "Laughter"];

    private static readonly TimeSpan EyesTick = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan EyesCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BlindDuration = TimeSpan.FromSeconds(5);

    private const float EyesRange = 7f;
    private const float BurstRange = 1f;
    private const float BurstHeat = 30f;
    private const float WordChance = 0.15f;
    private const float LiverChance = 0.2f;
    private const float LiverAmount = 5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticCorruptedOrganComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<StoriesHereticCorruptedBodyComponent, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<StoriesHereticCorruptedBodyComponent, IngestingEvent>(OnIngesting);
    }

    private bool HasKind(EntityUid body, StoriesHereticCorruptedOrganKind? kind)
    {
        if (!_containers.TryGetContainer(body, BodyComponent.ContainerID, out var container))
            return false;

        foreach (var organ in container.ContainedEntities)
        {
            if (TryComp<StoriesHereticCorruptedOrganComponent>(organ, out var comp) && (kind == null || comp.Kind == kind))
                return true;
        }

        return false;
    }

    private void OnRemoved(Entity<StoriesHereticCorruptedOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        var body = args.Target;
        if (TerminatingOrDeleted(body))
            return;

        // Орган уже вышел из контейнера: если других испорченных не осталось, маркер не нужен.
        if (!HasKind(body, null))
            RemComp<StoriesHereticCorruptedBodyComponent>(body);

        if (!_mobState.IsAlive(body) || IsSuppressed(body) || !_random.Prob(ent.Comp.BurstChance))
            return;

        var coords = _transform.GetMapCoordinates(body);
        var heat = new DamageSpecifier();
        heat.DamageDict["Heat"] = BurstHeat;
        foreach (var target in _lookup.GetEntitiesInRange<DamageableComponent>(coords, BurstRange))
        {
            _damageable.TryChangeDamage(target.AsNullable(), heat, origin: ent);
        }

        Spawn(BlastEffect, coords);
        _popup.PopupEntity(Loc.GetString("stories-heretic-organs-burst", ("organ", ent.Owner)), body, PopupType.MediumCaution);
        QueueDel(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticCorruptedOrganComponent, OrganComponent>();
        while (query.MoveNext(out _, out var corrupted, out var organ))
        {
            if (corrupted.Kind != StoriesHereticCorruptedOrganKind.Eyes || organ.Body is not { } body
                || now < corrupted.NextEffect)
                continue;

            corrupted.NextEffect = now + EyesTick;
            if (IsSuppressed(body) || FindWatchedHeretic(body) is not { } heretic)
                continue;

            corrupted.NextEffect = now + EyesCooldown;
            _statusEffects.TryUpdateStatusEffectDuration(body, Blindness, BlindDuration);
            _popup.PopupEntity(Loc.GetString("stories-heretic-organs-eyes-burn", ("heretic", heretic)), body, body, PopupType.MediumCaution);
        }
    }

    private EntityUid? FindWatchedHeretic(EntityUid body)
    {
        foreach (var heretic in _lookup.GetEntitiesInRange<StoriesHereticComponent>(_transform.GetMapCoordinates(body), EyesRange))
        {
            if (heretic.Owner != body && _heretic.HasFocus(heretic) && _examine.InRangeUnOccluded(body, heretic, EyesRange))
                return heretic;
        }

        return null;
    }

    private void OnAccent(Entity<StoriesHereticCorruptedBodyComponent> ent, ref AccentGetEvent args)
    {
        if (!HasKind(ent, StoriesHereticCorruptedOrganKind.Tongue) || IsSuppressed(ent))
            return;

        var dataset = _proto.Index(Shadowspeak);
        var words = args.Message.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            if (_random.Prob(WordChance))
                words[i] = _random.Pick(dataset);
        }

        args.Message = string.Join(' ', words);
    }

    private void OnIngesting(Entity<StoriesHereticCorruptedBodyComponent> ent, ref IngestingEvent args)
    {
        if (!HasKind(ent, StoriesHereticCorruptedOrganKind.Liver) || IsSuppressed(ent) || !_random.Prob(LiverChance))
            return;

        args.Split.AddReagent(_random.Pick(LiverReagents), LiverAmount);
        _popup.PopupEntity(Loc.GetString("stories-heretic-organs-liver-bubble"), ent, ent);
    }
}
