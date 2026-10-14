using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Atmos;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Dataset;
using Content.Shared.Examine;
using Content.Shared.Medical;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Stunnable;
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
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private VomitSystem _vomit = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;

    private static readonly EntProtoId BlastEffect = "STHereticCurseBlast";
    private static readonly EntProtoId Blindness = "StatusEffectBlindness";
    private static readonly ProtoId<DatasetPrototype> Shadowspeak = "STHereticShadowspeak";

    /// <summary>
    /// Реагенты, которые испорченная печень подмешивает в выпитое. SpaceMirage в проекте нет — отброшен.
    /// </summary>
    private static readonly string[] LiverReagents = ["Rum", "Vodka", "Happiness", "MindbreakerToxin", "Laughter"];

    /// <summary>
    /// Реагенты крови (как у лужи крови): выпив любой из них, испорченный желудок на время успокаивается.
    /// </summary>
    private static readonly string[] BloodReagents = ["Blood", "InsectBlood", "CopperBlood", "AmmoniaBlood"];

    // Миазмов в этом форке нет — вместо них аммиак (тоже вонючее облако).
    private static readonly Gas[] CoughGases = [Gas.Ammonia, Gas.Plasma];

    private static readonly TimeSpan SecondTick = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StomachMessageCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SatedDuration = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan AppendixKnockdown = TimeSpan.FromSeconds(0.5);

    private TimeSpan _nextSecond;

    // База голода человека -1/60 в секунду; ещё -2/60 даёт итоговые x3.
    private const float StomachExtraHunger = -2f / 60f;
    private const float HeartStamina = 20f;
    private const float LungsChance = 0.15f;
    private const float LungsMoles = 30f;
    private const float AppendixChance = 0.02f;

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
        var second = now >= _nextSecond;
        if (second)
            _nextSecond = now + SecondTick;

        var query = EntityQueryEnumerator<StoriesHereticCorruptedOrganComponent, OrganComponent>();
        while (query.MoveNext(out _, out var corrupted, out var organ))
        {
            if (organ.Body is not { } body || !_mobState.IsAlive(body))
                continue;

            switch (corrupted.Kind)
            {
                case StoriesHereticCorruptedOrganKind.Eyes when now >= corrupted.NextEffect:
                    corrupted.NextEffect = now + EyesTick;
                    if (IsSuppressed(body) || FindWatchedHeretic(body) is not { } heretic)
                        break;

                    corrupted.NextEffect = now + EyesCooldown;
                    _statusEffects.TryUpdateStatusEffectDuration(body, Blindness, BlindDuration);
                    _popup.PopupEntity(Loc.GetString("stories-heretic-organs-eyes-burn", ("heretic", heretic)), body, body, PopupType.MediumCaution);
                    break;
                case StoriesHereticCorruptedOrganKind.Heart when now >= corrupted.NextEffect:
                    corrupted.NextEffect = now + TimeSpan.FromSeconds(_random.Next(6, 46));
                    if (IsSuppressed(body))
                        break;

                    _stamina.TakeStaminaDamage(body, HeartStamina);
                    _popup.PopupEntity(Loc.GetString("stories-heretic-organs-heart-hand"), body, body, PopupType.MediumCaution);
                    break;
                case StoriesHereticCorruptedOrganKind.Stomach when second && now >= corrupted.SatedUntil && !IsSuppressed(body):
                    if (TryComp<SatiationComponent>(body, out var satiation))
                        _satiation.ModifyValue((body, satiation), SatiationSystem.Hunger, StomachExtraHunger);

                    if (now >= corrupted.NextEffect)
                    {
                        corrupted.NextEffect = now + StomachMessageCooldown;
                        _popup.PopupEntity(Loc.GetString("stories-heretic-organs-stomach-blood"), body, body);
                    }

                    break;
                case StoriesHereticCorruptedOrganKind.Lungs when second && HasComp<RespiratorComponent>(body)
                    && !IsSuppressed(body) && _random.Prob(LungsChance):
                    _atmosphere.GetContainingMixture(body, excite: true)?.AdjustMoles(_random.Pick(CoughGases), LungsMoles);
                    _popup.PopupEntity(Loc.GetString("stories-heretic-organs-lungs-cough"), body, body, PopupType.MediumCaution);
                    break;
                case StoriesHereticCorruptedOrganKind.Appendix when second && !IsSuppressed(body) && _random.Prob(AppendixChance):
                    _vomit.Vomit(body);
                    _stun.TryKnockdown(body, AppendixKnockdown);
                    break;
            }
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

    private static bool ContainsBlood(Solution split) => BloodReagents.Any(r => split.ContainsReagent(r, null));

    private void OnIngesting(Entity<StoriesHereticCorruptedBodyComponent> ent, ref IngestingEvent args)
    {
        if (ContainsBlood(args.Split)
            && _containers.TryGetContainer(ent, BodyComponent.ContainerID, out var bodyOrgans))
        {
            foreach (var organ in bodyOrgans.ContainedEntities)
            {
                if (TryComp<StoriesHereticCorruptedOrganComponent>(organ, out var comp) && comp.Kind == StoriesHereticCorruptedOrganKind.Stomach)
                    comp.SatedUntil = _timing.CurTime + SatedDuration;
            }
        }

        if (!HasKind(ent, StoriesHereticCorruptedOrganKind.Liver) || IsSuppressed(ent) || !_random.Prob(LiverChance))
            return;

        args.Split.AddReagent(_random.Pick(LiverReagents), LiverAmount);
        _popup.PopupEntity(Loc.GetString("stories-heretic-organs-liver-bubble"), ent, ent);
    }
}
