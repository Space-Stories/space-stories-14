#pragma warning disable RA0002
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Server._Stories.Heretic.Flesh;
using Content.Server._Stories.Heretic.Ghouls;
using Content.Shared.Body;
using Content.Shared.Body.Components;
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

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticFleshTest : GameTest
{
    [Test]
    public async Task FleshRootGrantsBladeAndSummonObjective()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var puddles = Server.System<SharedPuddleSystem>();
        var minds = Server.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var mindId = minds.CreateMind(null);
            minds.TransferTo(mindId, uid);
            Server.System<RoleSystem>().MindAddRole(mindId, "STMindRoleHeretic");
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));

            var points = heretic.Comp.Points;
            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshRoot"), Is.True);
            Assert.That(heretic.Comp.Points, Is.EqualTo(points - 1), "Корень стоит 1 очко");
            Assert.That(heretic.Comp.Path?.Id, Is.EqualTo("STHereticPathFlesh"));
            Assert.That(heretic.Comp.RequiredSummons, Is.EqualTo(2));
            Assert.That(heretic.Comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualFleshBlade"));

            var mind = SEntMan.GetComponent<MindComponent>(mindId);
            Assert.That(mind.Objectives.Any(o => SEntMan.GetComponent<MetaDataComponent>(o).EntityPrototype?.ID == "STHereticObjectiveSummon"));

            var rune = SSpawnAtPosition("STHereticRune", coords);
            for (var i = 1; i <= 4; i++)
            {
                SSpawnAtPosition("KitchenKnife", coords);
                Assert.That(puddles.TrySpillAt(coords, new Solution("Blood", 10), out _, sound: false), Is.True);
                Assert.That(rituals.TryPerform(heretic, rune), Is.True, $"Клинок {i}");
                Assert.That(heretics.CountLimitedResults(heretic, "STHereticRitualFleshBlade"), Is.EqualTo(i));
            }

            SSpawnAtPosition("KitchenKnife", coords);
            Assert.That(puddles.TrySpillAt(coords, new Solution("Blood", 10), out _, sound: false), Is.True);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "5-й клинок сверх лимита");
            Assert.That(heretics.CountLimitedResults(heretic, "STHereticRitualFleshBlade"), Is.EqualTo(4));
        });
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
}
