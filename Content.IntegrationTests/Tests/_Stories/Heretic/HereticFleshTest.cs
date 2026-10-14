#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
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
}
