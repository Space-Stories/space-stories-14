#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Server._Stories.Heretic.Ghouls;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech;
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
}
