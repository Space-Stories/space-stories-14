#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Fluids;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

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
}
