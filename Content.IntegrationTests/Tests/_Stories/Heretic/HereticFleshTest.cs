#pragma warning disable RA0002
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Server._Stories.Heretic.Flesh;
using Content.Server._Stories.Heretic.Ghouls;
using Content.Shared.Body;
using Content.Shared._Stories.Heretic.Flesh;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Inventory;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Mind.Components;
using Content.Shared.Body.Components;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Interaction;
using Content.Shared.Mindshield.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Content.Server._Stories.Heretic.Summons;
using Robust.Shared.Containers;
using Robust.Shared.Localization;
using Content.Shared.StatusEffectNew;
using Content.Shared.Chemistry.Components;
using Content.Shared.Fluids;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.UnitTesting.Pool;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticFleshTest : GameTest
{
    [Test]
    public async Task FleshRootGrantsBladeAndSummonObjective()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var minds = Server.System<MindSystem>();
        var heretic = default(Entity<StoriesHereticComponent>);
        var rune = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var mindId = minds.CreateMind(null);
            minds.TransferTo(mindId, uid);
            Server.System<RoleSystem>().MindAddRole(mindId, "STMindRoleHeretic");
            heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));

            var points = heretic.Comp.Points;
            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshRoot"), Is.True);
            Assert.That(heretic.Comp.Points, Is.EqualTo(points - 1), "Корень стоит 1 очко");
            Assert.That(heretic.Comp.Path?.Id, Is.EqualTo("STHereticPathFlesh"));
            Assert.That(heretic.Comp.RequiredSummons, Is.EqualTo(2));
            Assert.That(heretic.Comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualFleshBlade"));

            var mind = SEntMan.GetComponent<MindComponent>(mindId);
            Assert.That(mind.Objectives.Any(o => SEntMan.GetComponent<MetaDataComponent>(o).EntityPrototype?.ID == "STHereticObjectiveSummon"));

            rune = SSpawnAtPosition("STHereticRune", coords);
        });

        for (var i = 1; i <= 5; i++)
        {
            Assert.That(await TryBladeRitual(heretic, rune, testMap), Is.EqualTo(i <= 4), $"Клинок {i}");
            await Server.WaitAssertion(() =>
                Assert.That(heretics.CountLimitedResults(heretic, "STHereticRitualFleshBlade"), Is.EqualTo(Math.Min(i, 4))));
        }
    }

    [Test]
    public async Task GraspOfFleshRaisesGhoul()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var damageable = Server.System<DamageableSystem>();
        var mobState = Server.System<MobStateSystem>();
        var minds = Server.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            heretic.Comp.Points = 2;
            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshRoot"), Is.True);
            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshGrasp"), Is.True);

            // Игрок в тесте один: каждый новый труп забирает его разум себе.
            EntityUid Corpse()
            {
                var corpse = SSpawnAtPosition("MobHuman", coords);
                minds.TransferTo(minds.CreateMind(ServerSession!.UserId), corpse);
                damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
                return corpse;
            }

            void Hit(EntityUid target)
            {
                var ev = new StoriesHereticGraspHitEvent(uid, target);
                SEntMan.EventBus.RaiseLocalEvent(uid, ref ev);
            }

            var alive = SSpawnAtPosition("MobHuman", coords);
            Hit(alive);
            Assert.That(heretics.IsServant(alive, uid), Is.False, "Живой стал гулем");

            for (var i = 0; i < 3; i++)
            {
                var corpse = Corpse();
                Hit(corpse);
                Assert.That(mobState.IsAlive(corpse), Is.True, $"Гуль {i + 1} не ожил");
                Assert.That(heretics.IsServant(corpse, uid), Is.True);
            }

            var fourth = Corpse();
            Hit(fourth);
            Assert.That(mobState.IsDead(fourth), Is.True, "4-й гуль сверх лимита 3");
            Assert.That(heretics.IsServant(fourth, uid), Is.False);

            heretic.Comp.Ascended = true;
            Hit(fourth);
            Assert.That(heretics.IsServant(fourth, uid), Is.True, "После Вознесения лимит 6");
        });
    }

    private Entity<StoriesHereticComponent> LearnImperfect(EntityUid uid)
    {
        var heretics = Server.System<StoriesHereticSystem>();
        var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
        heretic.Comp.Points = 3;
        Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshRoot"), Is.True);
        Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshGrasp"), Is.True);
        Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshImperfect"), Is.True);
        Assert.That(heretic.Comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualFleshImperfect"));
        return heretic;
    }

    private EntityUid DeadCorpse(EntityCoordinates coords, bool soul)
    {
        var minds = Server.System<MindSystem>();
        var corpse = SSpawnAtPosition("MobHuman", coords);
        if (soul)
            minds.TransferTo(minds.CreateMind(ServerSession!.UserId), corpse);
        Server.System<DamageableSystem>().TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
        return corpse;
    }

    private int CountPoppies()
    {
        var count = 0;
        var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var poppy, out var meta))
        {
            if (meta.EntityPrototype?.ID == "FoodPoppy" && meta.EntityLifeStage < EntityLifeStage.Terminating && !SEntMan.IsQueuedForDeletion(poppy))
                count++;
        }

        return count;
    }

    [Test]
    public async Task ImperfectRitualRaisesVoiceless()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var thresholds = Server.System<MobThresholdSystem>();
        var mobState = Server.System<MobStateSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = LearnImperfect(uid);
            var rune = SSpawnAtPosition("STHereticRune", coords);
            var corpse = DeadCorpse(coords, true);
            SSpawnAtPosition("FoodPoppy", coords);

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(CountPoppies(), Is.EqualTo(0), "Мак поглощён");
            Assert.That(SEntMan.Deleted(corpse), Is.False, "Труп не должен поглощаться");
            Assert.That(mobState.IsAlive(corpse), Is.True);
            Assert.That(heretics.IsServant(corpse, uid), Is.True);
            Assert.That(thresholds.TryGetThresholdForState(corpse, MobState.Critical, out var crit) && crit == 50, Is.True);
            Assert.That(thresholds.TryGetThresholdForState(corpse, MobState.Dead, out var dead) && dead == 100, Is.True);

            var speak = new SpeakAttemptEvent(corpse);
            SEntMan.EventBus.RaiseLocalEvent(corpse, speak, true);
            Assert.That(speak.Cancelled, Is.True, "Безгласный мертвец заговорил");
            Assert.That(Server.System<StatusEffectsSystem>().HasStatusEffect(corpse, "StatusEffectMuted"), Is.True, "Нет эффекта немоты");
        });
    }

    [Test]
    public async Task ImperfectRitualOffersSoullessCorpse()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var minds = Server.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = LearnImperfect(uid);
            var rune = SSpawnAtPosition("STHereticRune", coords);
            var corpse = DeadCorpse(coords, false);
            SSpawnAtPosition("FoodPoppy", coords);

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(SEntMan.HasComponent<GhostRoleComponent>(corpse), Is.True, "Нет призрачной роли");
            Assert.That(SEntMan.GetComponent<GhostRoleComponent>(corpse).RaffleConfig, Is.Not.Null, "Роль без розыгрыша");
            Assert.That(heretics.IsServant(corpse, uid), Is.False);
            Assert.That(CountPoppies(), Is.EqualTo(0));

            minds.TransferTo(minds.CreateMind(ServerSession!.UserId), corpse);
            Assert.That(heretics.IsServant(corpse, uid), Is.True, "Игрок занял тело, но гуля нет");
            Assert.That(SEntMan.HasComponent<GhostRoleComponent>(corpse), Is.False);
            Assert.That(SEntMan.HasComponent<StoriesHereticGhoulOfferComponent>(corpse), Is.False);
        });
    }

    /// <summary>
    /// Штатный путь призрачной роли: проверка бана по роли гуля, затем ровно одна роль гуля на разуме.
    /// </summary>
    [Test]
    public async Task ImperfectOfferTakenThroughGhostRoleSystem()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var ghostRoles = Server.System<GhostRoleSystem>();
        var minds = Server.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = LearnImperfect(uid);
            var rune = SSpawnAtPosition("STHereticRune", coords);
            var corpse = DeadCorpse(coords, false);
            SSpawnAtPosition("FoodPoppy", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            var role = SEntMan.GetComponent<GhostRoleComponent>(corpse);
            Assert.That(role.RoleName, Is.EqualTo(Loc.GetString("stories-heretic-flesh-voiceless-name")));
            Assert.That(role.MindRoles, Is.EqualTo(new List<EntProtoId> { "STMindRoleHereticGhoul" }), "Бан на роль гуля не проверяется");

            Assert.That(ghostRoles.Takeover(ServerSession!, role.Identifier), Is.True);
            Assert.That(heretics.IsServant(corpse, uid), Is.True, "Игрок занял тело, но гуля нет");
            Assert.That(SEntMan.HasComponent<StoriesHereticGhoulComponent>(corpse), Is.True);
            Assert.That(minds.TryGetMind(corpse, out _, out var mind), Is.True);
            var ghoulRoles = mind!.MindRoleContainer.ContainedEntities
                .Count(r => SEntMan.GetComponent<MetaDataComponent>(r).EntityPrototype?.ID == "STMindRoleHereticGhoul");
            Assert.That(ghoulRoles, Is.EqualTo(1), "Роль гуля на разуме не одна");
        });
    }

    [Test]
    public async Task ImperfectOfferWithDeletedMasterRefunds()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var minds = Server.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = LearnImperfect(uid);
            var rune = SSpawnAtPosition("STHereticRune", coords);
            var corpse = DeadCorpse(coords, false);
            SSpawnAtPosition("FoodPoppy", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            SEntMan.DeleteEntity(uid);
            minds.TransferTo(minds.CreateMind(ServerSession!.UserId), corpse);
            Assert.That(SEntMan.HasComponent<StoriesHereticGhoulComponent>(corpse), Is.False, "Гуль без хозяина");
            Assert.That(SEntMan.HasComponent<StoriesHereticGhoulOfferComponent>(corpse), Is.False);
            Assert.That(CountPoppies(), Is.EqualTo(1), "Мак не вернулся");
        });
    }

    [Test]
    public async Task ImperfectRitualOfferExpiresAndReturnsPoppy()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var timing = Server.ResolveDependency<IGameTiming>();
        EntityUid corpse = default;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = LearnImperfect(SSpawnAtPosition("MobHuman", coords));
            var rune = SSpawnAtPosition("STHereticRune", coords);
            corpse = DeadCorpse(coords, false);
            SSpawnAtPosition("FoodPoppy", coords);

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(CountPoppies(), Is.EqualTo(0));
            SEntMan.GetComponent<StoriesHereticGhoulOfferComponent>(corpse).ExpireTime = timing.CurTime;
        });

        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<GhostRoleComponent>(corpse), Is.False);
            Assert.That(SEntMan.HasComponent<StoriesHereticGhoulOfferComponent>(corpse), Is.False);
            Assert.That(CountPoppies(), Is.EqualTo(1), "Мак не вернулся");
            Assert.That(Server.System<MobStateSystem>().IsDead(corpse), Is.True, "Труп должен остаться трупом");
        });
    }

    [Test]
    public async Task ImperfectRitualLimitIsTwo()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = LearnImperfect(uid);
            var rune = SSpawnAtPosition("STHereticRune", coords);

            for (var i = 0; i < 2; i++)
            {
                DeadCorpse(coords, true);
                SSpawnAtPosition("FoodPoppy", coords);
                Assert.That(rituals.TryPerform(heretic, rune), Is.True, $"Гуль {i + 1}");
            }

            var third = DeadCorpse(coords, true);
            SSpawnAtPosition("FoodPoppy", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "3-й гуль сверх лимита 2");
            Assert.That(heretics.IsServant(third, uid), Is.False);

            heretic.Comp.Ascended = true;
            Assert.That(rituals.TryPerform(heretic, rune), Is.True, "После Вознесения лимит 6");
            Assert.That(heretics.IsServant(third, uid), Is.True);
        });
    }

    [Test]
    public async Task ImperfectOffersCountTowardLimit()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = LearnImperfect(SSpawnAtPosition("MobHuman", coords));
            var rune = SSpawnAtPosition("STHereticRune", coords);
            var corpses = new[] { DeadCorpse(coords, false), DeadCorpse(coords, false), DeadCorpse(coords, false) };
            for (var i = 0; i < 3; i++)
                SSpawnAtPosition("FoodPoppy", coords);

            Assert.That(rituals.TryPerform(heretic, rune), Is.True, "Предложение 1");
            Assert.That(rituals.TryPerform(heretic, rune), Is.True, "Предложение 2 на следующем свободном трупе");
            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "3-е предложение сверх лимита 2");
            Assert.That(corpses.Count(c => SEntMan.HasComponent<StoriesHereticGhoulOfferComponent>(c)), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ImperfectOfferRefundsWhenCorpseDeleted()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = LearnImperfect(SSpawnAtPosition("MobHuman", coords));
            var rune = SSpawnAtPosition("STHereticRune", coords);
            var corpse = DeadCorpse(coords, false);
            SSpawnAtPosition("FoodPoppy", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(CountPoppies(), Is.EqualTo(0));

            SEntMan.DeleteEntity(corpse);
            Assert.That(CountPoppies(), Is.EqualTo(1), "Мак не вернулся после удаления трупа");
        });
    }

    [Test]
    public async Task FleshMarkAddsBleedingAndSlash()
    {
        var testMap = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = SSpawnAtPosition("MobHuman", coords);
            var target = SSpawnAtPosition("MobHuman", coords);
            SEntMan.AddComponent<StoriesHereticFleshMarkComponent>(heretic);
            var bloodstream = SEntMan.GetComponent<BloodstreamComponent>(target);
            // Контроль: цель, получившая только тот же Slash. Сам урон тоже вызывает кровотечение.
            var control = SSpawnAtPosition("MobHuman", coords);
            var heretic2 = SEntMan.GetComponent<StoriesHereticFleshMarkComponent>(heretic);
            Server.System<DamageableSystem>().TryChangeDamage(control, heretic2.Damage, true);
            var controlBleed = SEntMan.GetComponent<BloodstreamComponent>(control).BleedAmount;

            var ev = new StoriesHereticMarkTriggeredEvent(heretic, target, "STHereticPathFlesh", 0);
            SEntMan.EventBus.RaiseEvent(EventSource.Local, ref ev);

            Assert.That(bloodstream.BleedAmount, Is.EqualTo(Math.Min(bloodstream.MaxBleedAmount, controlBleed + 8f)), "+8 от метки поверх кровотечения от Slash, не выше MaxBleedAmount");
            Assert.That(SEntMan.GetComponent<DamageableComponent>(target).Damage.DamageDict.TryGetValue("Slash", out var slash) && slash > 0, Is.True);
        });
    }

    [Test]
    public async Task BleedingSteelAddsBleedingOnlyToLiving()
    {
        var testMap = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = SSpawnAtPosition("MobHuman", coords);
            var blade = SSpawnAtPosition("STHereticBladeFlesh", coords);
            var target = SSpawnAtPosition("MobHuman", coords);
            SEntMan.AddComponent<StoriesHereticBladeBleedComponent>(heretic);
            var bloodstream = SEntMan.GetComponent<BloodstreamComponent>(target);
            var before = bloodstream.BleedAmount;

            var ev = new StoriesHereticBladeHitEvent(heretic, target, blade);
            SEntMan.EventBus.RaiseLocalEvent(heretic, ref ev);
            Assert.That(bloodstream.BleedAmount, Is.EqualTo(before + 4f));

            Server.System<DamageableSystem>().TryChangeDamage(target, new DamageSpecifier { DamageDict = new() { { "Blunt", 500 } } }, true);
            before = bloodstream.BleedAmount;
            SEntMan.EventBus.RaiseLocalEvent(heretic, ref ev);
            Assert.That(bloodstream.BleedAmount, Is.EqualTo(before), "По мёртвым без эффекта");
        });
    }
    [Test]
    public async Task KnitFleshHealsServantOnly()
    {
        var testMap = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = SSpawnAtPosition("MobHuman", coords);
            var servant = SSpawnAtPosition("MobHuman", coords);
            var stranger = SSpawnAtPosition("MobHuman", coords);
            SEntMan.AddComponent<StoriesHereticKnitFleshComponent>(heretic);
            Server.System<StoriesHereticSystem>().MakeServant(servant, heretic);
            var dmg = new DamageSpecifier { DamageDict = new() { { "Blunt", 30 } } };
            Server.System<DamageableSystem>().TryChangeDamage(servant, dmg, true);
            Server.System<DamageableSystem>().TryChangeDamage(stranger, dmg, true);

            float Blunt(EntityUid uid) => (float) SEntMan.GetComponent<DamageableComponent>(uid).Damage.DamageDict["Blunt"];

            var ev = new StoriesHereticKnitFleshEvent { Performer = heretic, Target = stranger };
            SEntMan.EventBus.RaiseLocalEvent(heretic, (object) ev);
            Assert.That(ev.Handled, Is.False, "Чужих не лечим и перезарядку не тратим");
            Assert.That(Blunt(stranger), Is.EqualTo(30f));

            ev = new StoriesHereticKnitFleshEvent { Performer = heretic, Target = servant };
            SEntMan.EventBus.RaiseLocalEvent(heretic, (object) ev);
            Assert.That(ev.Handled, Is.True);
            Assert.That(Blunt(servant), Is.EqualTo(20f));
        });
    }

    [Test]
    public async Task KnitFleshVerbsAndExtraction()
    {
        var testMap = await Pair.CreateTestMap();
        var verbs = Server.System<SharedVerbSystem>();
        var knit = Server.System<StoriesHereticFleshSystem>();
        var containers = Server.System<SharedContainerSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = SSpawnAtPosition("MobHuman", coords);
            var victim = SSpawnAtPosition("MobHuman", coords);
            SEntMan.AddComponent<StoriesHereticKnitFleshComponent>(heretic);
            SEntMan.AddComponent<StoriesHereticComponent>(heretic).Ascended = true;
            var loc = Server.ResolveDependency<ILocalizationManager>();
            var liver = loc.GetString("stories-heretic-knit-verb", ("part", loc.GetString("stories-heretic-knit-part-Liver")));
            var heart = loc.GetString("stories-heretic-knit-verb", ("part", loc.GetString("stories-heretic-knit-part-Heart")));

            bool Has(string text) => verbs.GetLocalVerbs(victim, heretic, typeof(AlternativeVerb), force: true).Any(v => v.Text == text);

            Assert.That(Has(heart), Is.False, "Сердце у живого не достать");
            Assert.That(Has(liver), Is.True, "Печень у живого можно");

            Server.System<DamageableSystem>().TryChangeDamage(victim, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            Assert.That(Has(heart), Is.True, "Сердце у трупа можно");
            Assert.That(Has(liver), Is.True);

            EntityUid? Find(string cat) => containers.GetContainer(victim, BodyComponent.ContainerID).ContainedEntities
                .Cast<EntityUid?>().FirstOrDefault(o => SEntMan.GetComponent<OrganComponent>(o!.Value).Category == cat);

            Assert.That(Find("Brain"), Is.Not.Null);
            var organ = Find("Liver")!.Value;
            Assert.That(knit.Extract(heretic, victim, organ), Is.True);
            Assert.That(Find("Liver"), Is.Null, "Печень осталась в теле");
        });
    }

    [Test]
    public async Task RawRitualSummonsProphet()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var dismember = Server.System<StoriesHereticDismemberSystem>();
        var puddles = Server.System<SharedPuddleSystem>();
        var containers = Server.System<SharedContainerSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            heretic.Comp.Points = 20;
            foreach (var id in new[]
                     {
                         "Root", "Grasp", "Imperfect", "Mark", "Knitting", "Raw",
                     })
            {
                Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFlesh" + id), Is.True, id);
            }

            Assert.That(heretic.Comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualRawProphet"));

            var corpse = SSpawnAtPosition("MobHuman", coords);
            Server.System<DamageableSystem>().TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            EntityUid Find(string cat) => containers.GetContainer(corpse, BodyComponent.ContainerID).ContainedEntities
                .First(o => SEntMan.GetComponent<OrganComponent>(o).Category == cat);

            var rune = SSpawnAtPosition("STHereticRune", coords);
            Assert.That(containers.Remove(Find("Eyes"), containers.GetContainer(corpse, BodyComponent.ContainerID), destination: coords), Is.True);
            Assert.That(puddles.TrySpillAt(coords, new Solution("Blood", 10), out _, sound: false), Is.True);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "Без руки ритуал сработал");

            Assert.That(dismember.Dismember(corpse, Find("ArmLeft")), Is.Not.Null);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent, MetaDataComponent>();
            Assert.That(query.MoveNext(out _, out _, out var meta), Is.True, "Моб не призван");
            Assert.That(meta.EntityPrototype?.ID, Is.EqualTo("STHereticMobRawProphet"));
        });
    }

    [Test]
    public async Task FocusedAttackBonusGrowsAndResets()
    {
        var testMap = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var prophet = SSpawnAtPosition("STHereticMobRawProphet", coords);
            var a = SSpawnAtPosition("MobHuman", coords);
            var b = SSpawnAtPosition("MobHuman", coords);

            float Hit(EntityUid target)
            {
                var ev = new MeleeHitEvent(new HashSet<EntityUid> { target }, prophet, prophet, new DamageSpecifier(), null);
                SEntMan.EventBus.RaiseLocalEvent(prophet, ev);
                return ev.BonusDamage.DamageDict.GetValueOrDefault("Blunt").Float();
            }

            Assert.That(Hit(a), Is.EqualTo(0f));
            Assert.That(Hit(a), Is.EqualTo(2f));
            Assert.That(Hit(a), Is.EqualTo(4f));
            for (var i = 0; i < 10; i++)
                Hit(a);
            Assert.That(Hit(a), Is.EqualTo(10f), "Потолок бонуса");
            Assert.That(Hit(b), Is.EqualTo(0f), "Смена цели не сбросила бонус");
            Assert.That(Hit(b), Is.EqualTo(2f));
        });
    }

    [Test]
    public async Task UnclaimedProphetReturnsDetachedArm()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var dismember = Server.System<StoriesHereticDismemberSystem>();
        var puddles = Server.System<SharedPuddleSystem>();
        var containers = Server.System<SharedContainerSystem>();
        var timing = Server.ResolveDependency<IGameTiming>();
        var armIngredient = new StoriesHereticIngredient { OrganCategory = "ArmLeft" };

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            heretic.Comp.Points = 20;
            foreach (var id in new[] { "Root", "Grasp", "Imperfect", "Mark", "Knitting", "Raw" })
                Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFlesh" + id), Is.True, id);

            var corpse = SSpawnAtPosition("MobHuman", coords);
            Server.System<DamageableSystem>().TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            EntityUid Find(string cat) => containers.GetContainer(corpse, BodyComponent.ContainerID).ContainedEntities
                .First(o => SEntMan.GetComponent<OrganComponent>(o).Category == cat);

            var rune = SSpawnAtPosition("STHereticRune", coords);
            Assert.That(containers.Remove(Find("Eyes"), containers.GetContainer(corpse, BodyComponent.ContainerID), destination: coords), Is.True);
            Assert.That(dismember.Dismember(corpse, Find("ArmLeft")), Is.Not.Null);
            Assert.That(puddles.TrySpillAt(coords, new Solution("Blood", 10), out _, sound: false), Is.True);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent>();
            Assert.That(query.MoveNext(out _, out var summon), Is.True);
            summon.ExpireTime = timing.CurTime;
        });

        await Pair.RunTicksSync(3);

        await Server.WaitAssertion(() =>
        {
            var found = false;
            var query = SEntMan.AllEntityQueryEnumerator<BodyComponent>();
            while (query.MoveNext(out var body, out _))
            {
                if (SEntMan.HasComponent<MobStateComponent>(body) || SEntMan.IsQueuedForDeletion(body)
                    || !containers.TryGetContainer(body, BodyComponent.ContainerID, out var container))
                    continue;

                if (container.ContainedEntities.Any(o => SEntMan.GetComponent<OrganComponent>(o).Category == "ArmLeft")
                    && rituals.Matches(armIngredient, body))
                    found = true;
            }

            Assert.That(found, Is.True, "Рука не вернулась в отделённом теле");
        });
    }

    [Test]
    public async Task MansusLinkGivesChannelAndBreaksOnProphetDeath()
    {
        var testMap = await Pair.CreateTestMap();
        var channel = (ProtoId<RadioChannelPrototype>) "STHereticMansusLink";
        var other = (ProtoId<RadioChannelPrototype>) "Common";
        EntityUid prophet = default, target = default, shielded = default;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            prophet = SSpawnAtPosition("STHereticMobRawProphet", coords);
            target = SSpawnAtPosition("MobHuman", coords);
            shielded = SSpawnAtPosition("MobHuman", coords);
            SEntMan.AddComponent<MindShieldComponent>(shielded);
            SEntMan.EnsureComponent<ActiveRadioComponent>(target).Channels.Add(other);

            foreach (var t in new[] { shielded, target })
            {
                var ev = new StoriesHereticMansusLinkEvent { Performer = prophet, Target = t, Duration = TimeSpan.FromSeconds(0.1f) };
                SEntMan.EventBus.RaiseLocalEvent(prophet, ev);
                Assert.That(ev.Handled, Is.EqualTo(t == target), "DoAfter: запуск");
            }
        });

        await Pair.RunTicksSync(15);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<ActiveRadioComponent>(target).Channels, Does.Contain(channel));
            Assert.That(SEntMan.GetComponent<IntrinsicRadioTransmitterComponent>(target).Channels, Does.Contain(channel));
            Assert.That(SEntMan.GetComponent<ActiveRadioComponent>(prophet).Channels, Does.Contain(channel), "У пророка нет канала");
            Assert.That(SEntMan.HasComponent<ActiveRadioComponent>(shielded), Is.False, "Майндшилд не защитил от связи");

            Server.System<DamageableSystem>().TryChangeDamage(prophet, new DamageSpecifier { DamageDict = new() { { "Blunt", 500 } } }, true);
            SEntMan.DeleteEntity(prophet);
        });

        await Pair.RunTicksSync(3);

        await Server.WaitAssertion(() =>
        {
            var active = SEntMan.GetComponent<ActiveRadioComponent>(target).Channels;
            Assert.That(active, Does.Not.Contain(channel));
            Assert.That(active, Does.Contain(other), "Чужой канал снят");
            Assert.That(SEntMan.GetComponent<IntrinsicRadioTransmitterComponent>(target).Channels, Does.Not.Contain(channel));
            Assert.That(SEntMan.HasComponent<StunnedComponent>(target), Is.True, "Нет паралича");
        });
    }

    [Test]
    public async Task ExpandSightTogglesZoom()
    {
        var testMap = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var prophet = SSpawnAtPosition("STHereticMobRawProphet", testMap.GridCoords);
            var eye = SEntMan.GetComponent<ContentEyeComponent>(prophet);
            for (var i = 0; i < 2; i++)
            {
                var ev = new StoriesHereticExpandSightEvent { Performer = prophet };
                SEntMan.EventBus.RaiseLocalEvent(prophet, ev);
                Assert.That(eye.TargetZoom.X, Is.EqualTo(i == 0 ? 1.5f : 1f).Within(0.001f));
            }
        });
    }

    [Test]
    public async Task LonelyRitualSummonsStalker()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            heretic.Comp.Points = 20;
            foreach (var id in new[]
                     {
                         "Root", "Grasp", "Imperfect", "Mark", "Knitting", "Raw", "BleedingSteel", "Lonely",
                     })
            {
                Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFlesh" + id), Is.True, id);
            }

            Assert.That(heretic.Comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualStalker"));

            var rune = SSpawnAtPosition("STHereticRune", coords);
            SSpawnAtPosition("FoodMeat", coords);
            SSpawnAtPosition("FoodMeat", coords);
            SSpawnAtPosition("Pen", coords);
            SSpawnAtPosition("Paper", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "Двух кусков мяса хватило");

            SSpawnAtPosition("FoodMeat", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent, MetaDataComponent>();
            Assert.That(query.MoveNext(out _, out _, out var meta), Is.True, "Моб не призван");
            Assert.That(meta.EntityPrototype?.ID, Is.EqualTo("STHereticMobStalker"));
        });
    }

    [Test]
    public async Task StalkerEmpDrainsBatteriesInRange()
    {
        var testMap = await Pair.CreateTestMap();
        var battery = Server.System<SharedBatterySystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var stalker = SSpawnAtPosition("STHereticMobStalker", coords);
            var cell = SSpawnAtPosition("PowerCellHigh", coords);
            var far = SSpawnAtPosition("PowerCellHigh", coords.Offset(new System.Numerics.Vector2(20, 0)));
            var before = battery.GetCharge((cell, SEntMan.GetComponent<BatteryComponent>(cell)));
            Assert.That(before, Is.GreaterThan(0f));

            var ev = new StoriesHereticEldritchEmpEvent { Range = 6f, Energy = 50000f, Duration = System.TimeSpan.FromSeconds(15) };
            SEntMan.EventBus.RaiseLocalEvent(stalker, ev, true);

            Assert.That(ev.Handled, Is.True);
            Assert.That(battery.GetCharge((cell, SEntMan.GetComponent<BatteryComponent>(cell))), Is.LessThan(before), "Батарея в радиусе не разрядилась");
            Assert.That(battery.GetCharge((far, SEntMan.GetComponent<BatteryComponent>(far))), Is.EqualTo(before), "Дальняя батарея разрядилась");
        });
    }

    [Test]
    public async Task StalkerFormChoiceMorphsOnceIntoCat()
    {
        var testMap = await Pair.CreateTestMap();
        var minds = Server.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            var stalker = SSpawnAtPosition("STHereticMobStalker", testMap.GridCoords);
            var mindId = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mindId, stalker);

            var form = SEntMan.GetComponent<StoriesHereticStalkerFormComponent>(stalker);
            Assert.That(form.Chosen, Is.Null);

            // Неизвестный облик игнорируется.
            SEntMan.EventBus.RaiseLocalEvent(stalker, new StoriesHereticStalkerFormSelectMessage("STHereticAshJaunt") { Actor = stalker }, true);
            Assert.That(form.Chosen, Is.Null);

            SEntMan.EventBus.RaiseLocalEvent(stalker, new StoriesHereticStalkerFormSelectMessage("STHereticStalkerFormCat") { Actor = stalker }, true);
            Assert.That(form.Chosen?.Id, Is.EqualTo("STHereticStalkerFormCat"));

            var mind = SEntMan.GetComponent<MindComponent>(mindId);
            var body = mind.OwnedEntity!.Value;
            Assert.That(body, Is.Not.EqualTo(stalker));
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(body).EntityPrototype?.ID, Is.EqualTo("MobCat"));
            Assert.That(SEntMan.HasComponent<PolymorphedEntityComponent>(body));
        });
    }

    [Test]
    public async Task StalkerFormIsServantAndDeathCollapsesStalker()
    {
        var testMap = await Pair.CreateTestMap();
        var minds = Server.System<MindSystem>();
        var heretics = Server.System<StoriesHereticSystem>();
        var damageable = Server.System<DamageableSystem>();
        var mobState = Server.System<MobStateSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var master = SSpawnAtPosition("MobHuman", coords);
            var stalker = SSpawnAtPosition("STHereticMobStalker", coords);
            var summon = SEntMan.GetComponent<StoriesHereticSummonComponent>(stalker);
            summon.Master = master;
            summon.Ingredients.Add("Pen");
            heretics.MakeServant(stalker, master);

            var mindId = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mindId, stalker);
            SEntMan.EventBus.RaiseLocalEvent(stalker, new StoriesHereticStalkerFormSelectMessage("STHereticStalkerFormCat") { Actor = stalker }, true);

            var cat = SEntMan.GetComponent<MindComponent>(mindId).OwnedEntity!.Value;
            Assert.That(cat, Is.Not.EqualTo(stalker));
            Assert.That(heretics.IsServant(cat, master), Is.True, "Облик не слуга хозяина");

            damageable.TryChangeDamage(cat, new DamageSpecifier { DamageDict = new() { { "Blunt", 500 } } }, true);

            Assert.That(SEntMan.GetComponent<MindComponent>(mindId).OwnedEntity, Is.EqualTo(stalker), "Нет возврата в Сталкера");
            Assert.That(mobState.IsDead(stalker), Is.True, "Сталкер не умер после смерти облика");
            Assert.That(summon.Collapsed, Is.True, "Призыв не схлопнулся");
            Assert.That(SEntMan.Deleted(stalker) || SEntMan.EntityQuery<MetaDataComponent>().Any(m => m.EntityPrototype?.ID == "Pen"), Is.True);
        });
    }

    [Test]
    public async Task FleshAscensionNeedsFourCorpsesAndLiftsLimits()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var damageable = Server.System<DamageableSystem>();
        var actions = Server.System<SharedActionsSystem>();
        var minds = Server.System<MindSystem>();
        var status = Server.System<StatusEffectsSystem>();
        var heretic = default(Entity<StoriesHereticComponent>);
        var mindId = EntityUid.Invalid;
        var rune = EntityUid.Invalid;

        EntityUid SpawnCorpse()
        {
            var corpse = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            return corpse;
        }

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            mindId = minds.CreateMind(null);
            minds.TransferTo(mindId, uid);
            heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            heretic.Comp.Points = 30;
            foreach (var id in new[] { "Root", "Grasp", "Imperfect", "Mark", "Knitting", "Raw", "BleedingSteel", "Lonely" })
                Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFlesh" + id), Is.True, id);

            heretic.Comp.SummonCount = heretic.Comp.RequiredSummons;
            heretic.Comp.SacrificeCount = heretic.Comp.RequiredSacrifices;
            heretic.Comp.HeadSacrificeCount = heretic.Comp.RequiredHeadSacrifices;
            heretic.Comp.LearnedCount = heretic.Comp.RequiredKnowledge;
            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshAscension"), Is.True);
            Assert.That(heretic.Comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualFleshAscension"));

            rune = SSpawnAtPosition("STHereticRune", coords);
            var corpses = new List<EntityUid> { SpawnCorpse(), SpawnCorpse(), SpawnCorpse() };

            // Трёх трупов мало: подсказка о недостающем.
            var nearby = new HashSet<EntityUid>(corpses);
            var ev = new StoriesHereticRitualEffectEvent(heretic, rune, "STHereticRitualFleshAscension",
                StoriesHereticRitualEffect.Ascension, nearby, new List<EntityUid>());
            SEntMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
            Assert.That(ev.Success, Is.False);
            Assert.That(ev.FailReason, Is.EqualTo(Loc.GetString("stories-heretic-flesh-ascension-corpses", ("count", 1))));
            Assert.That(rituals.TryPerform(heretic, rune), Is.False);
            Assert.That(heretic.Comp.Ascended, Is.False);

            corpses.Add(SpawnCorpse());
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(heretic.Comp.Ascended, Is.True);
            Assert.That(heretic.Comp.Rituals, Does.Not.Contain((EntProtoId) "STHereticRitualFleshAscension"));
            Assert.That(corpses.All(c => SEntMan.Deleted(c) || SEntMan.IsQueuedForDeletion(c)), "Трупы не поглощены");

        });

        await Pair.RunTicksSync(1);

        // Клинки без лимита (лимит до Вознесения — 4).
        for (var i = 1; i <= 5; i++)
            Assert.That(await TryBladeRitual(heretic, rune, testMap), Is.True, $"Клинок {i}");

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            Assert.That(heretics.CountLimitedResults(heretic, "STHereticRitualFleshBlade"), Is.EqualTo(5));

            // Повтор невозможен: ритуала больше нет.
            for (var i = 0; i < 4; i++)
                SpawnCorpse();
            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "Вознесение повторилось");

            // «Сбросить форму»: Владыка Ночи со штатным возвратом и ужасом свидетеля.
            var witness = SSpawnAtPosition("MobHuman", coords);
            Entity<ActionComponent>? shed = null;
            foreach (var action in actions.GetActions(heretic))
            {
                if (SEntMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID == "STActionHereticShedForm")
                    shed = action;
            }

            Assert.That(shed, Is.Not.Null, "Нет действия «Сбросить форму»");
            var shedEvent = (StoriesHereticShedFormEvent) SEntMan.GetComponent<InstantActionComponent>(shed!.Value).Event!;
            shedEvent.FearChance = 1f;
            actions.PerformAction(heretic.Owner, shed.Value, predicted: false);

            var lord = SEntMan.GetComponent<MindComponent>(mindId).OwnedEntity!.Value;
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(lord).EntityPrototype?.ID, Is.EqualTo("STHereticMobLordOfNight"));
            Assert.That(SEntMan.GetComponent<PolymorphedEntityComponent>(lord).Action, Is.Not.Null, "Нет действия возврата");
            Assert.That(status.HasStatusEffect(witness, "StatusEffectStutter"), Is.True, "Свидетель не напуган");
        });
    }

    private static readonly EntProtoId ShedFormAction = "STActionHereticShedForm";

    /// <summary>Еретик с разумом сбрасывает форму: возвращает Владыку Ночи.</summary>
    private EntityUid ShedForm(EntityUid uid)
    {
        var minds = Server.System<MindSystem>();
        var actions = Server.System<SharedActionsSystem>();
        var mindId = minds.CreateMind(null);
        minds.TransferTo(mindId, uid);
        SEntMan.AddComponent<StoriesHereticComponent>(uid);
        var shed = actions.AddAction(uid, ShedFormAction);
        Assert.That(shed, Is.Not.Null);
        actions.PerformAction(uid, (shed!.Value, SEntMan.GetComponent<ActionComponent>(shed.Value)), predicted: false);
        var lord = SEntMan.GetComponent<MindComponent>(mindId).OwnedEntity!.Value;
        Assert.That(SEntMan.GetComponent<MetaDataComponent>(lord).EntityPrototype?.ID, Is.EqualTo("STHereticMobLordOfNight"));
        return lord;
    }

    [Test]
    public async Task ShedFormKeepsInventory()
    {
        var testMap = await Pair.CreateTestMap();
        var inventory = Server.System<InventorySystem>();
        var polymorph = Server.System<PolymorphSystem>();

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            Assert.That(inventory.SpawnItemInSlot(uid, "id", "PassengerIDCard", true, true), Is.True);
            Assert.That(inventory.TryGetSlotEntity(uid, "id", out var card), Is.True);

            var lord = ShedForm(uid);
            Assert.That(polymorph.Revert(lord), Is.EqualTo(uid));
            Assert.That(inventory.TryGetSlotEntity(uid, "id", out var after) && after == card, Is.True, "Карта не осталась в слоте");
        });
    }

    [Test]
    public async Task LordOfNightDeathKillsHeretic()
    {
        var testMap = await Pair.CreateTestMap();
        var damageable = Server.System<DamageableSystem>();
        var mobState = Server.System<MobStateSystem>();
        var uid = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var lord = ShedForm(uid);
            damageable.TryChangeDamage(lord, new DamageSpecifier { DamageDict = new() { { "Slash", 1000 } } }, true);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<StoriesHereticComponent>(uid), Is.True);
            Assert.That(SEntMan.GetComponent<TransformComponent>(uid).MapID, Is.EqualTo(testMap.MapId), "Еретик не вернулся из формы");
            Assert.That(mobState.IsDead(uid), Is.True, "Смерть Владыки не убила еретика");
        });
    }

    [Test]
    public async Task LordOfNightEatsDetachedArm()
    {
        var testMap = await Pair.CreateTestMap();
        var damageable = Server.System<DamageableSystem>();
        var dismember = Server.System<StoriesHereticDismemberSystem>();
        var interaction = Server.System<SharedInteractionSystem>();
        var containers = Server.System<SharedContainerSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var lord = SSpawnAtPosition("STHereticMobLordOfNight", coords);
            damageable.TryChangeDamage(lord, new DamageSpecifier { DamageDict = new() { { "Slash", 600 } } }, true);

            var corpse = SSpawnAtPosition("MobHuman", coords);
            damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            EntityUid Find(string cat) => containers.GetContainer(corpse, BodyComponent.ContainerID).ContainedEntities
                .First(o => SEntMan.GetComponent<OrganComponent>(o).Category == cat);

            var leg = dismember.Dismember(corpse, Find("LegLeft"))!.Value;
            interaction.UserInteraction(lord, SEntMan.GetComponent<TransformComponent>(leg).Coordinates, leg);
            Assert.That(SEntMan.Deleted(leg) || SEntMan.IsQueuedForDeletion(leg), Is.False, "Съедена нога");

            var arm = dismember.Dismember(corpse, Find("ArmLeft"))!.Value;
            interaction.UserInteraction(lord, SEntMan.GetComponent<TransformComponent>(arm).Coordinates, arm);
            Assert.That(SEntMan.Deleted(arm) || SEntMan.IsQueuedForDeletion(arm), Is.True, "Рука не съедена");

            // 50% от 1000 по имеющемуся урону: из 600 Slash остаётся 100.
            Assert.That(SEntMan.GetComponent<DamageableComponent>(lord).TotalDamage.Float(), Is.EqualTo(100f).Within(0.1f));
        });
    }

    /// <summary>
    /// Нож и лужа крови на руне, затем ритуал. Готовый клинок (он тоже нож) уносится с руны,
    /// а тик удаляет поглощённое: иначе новая кровь вольётся в лужу, которая уже ждёт удаления.
    /// </summary>
    private async Task<bool> TryBladeRitual(Entity<StoriesHereticComponent> heretic, EntityUid rune, TestMapData map)
    {
        var result = false;
        await Server.WaitAssertion(() =>
        {
            SSpawnAtPosition("KitchenKnife", map.GridCoords);
            Assert.That(Server.System<SharedPuddleSystem>().TrySpillAt(map.GridCoords, new Solution("Blood", 10), out _, sound: false), Is.True);
            result = Server.System<StoriesHereticRitualSystem>().TryPerform(heretic, rune);

            var query = SEntMan.EntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out var uid, out var meta))
            {
                if (meta.EntityPrototype?.ID == "STHereticBladeFlesh")
                    Server.System<SharedTransformSystem>().SetMapCoordinates(uid, map.MapCoords.Offset(50, 50));
            }
        });
        await Pair.RunTicksSync(1);
        return result;
    }
}
