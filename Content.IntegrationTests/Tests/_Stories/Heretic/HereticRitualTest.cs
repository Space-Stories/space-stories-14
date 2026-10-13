#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticRitualTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: STTestHereticResult
  components:
  - type: Item

- type: entity
  id: STTestHereticRitualLimited
  components:
  - type: StoriesHereticRitual
    limit: 1
    ingredients:
    - name: stories-heretic-ingredient-knife
      whitelist:
        tags: [ Knife ]
    results:
    - STTestHereticResult
";

    [Test]
    public async Task RecipeConsumesAndRespectsLimit()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var heretic = new Entity<StoriesHereticComponent>(uid, comp);
            comp.Rituals.Clear();
            comp.Rituals.Add("STTestHereticRitualLimited");

            var rune = SSpawnAtPosition("STHereticRune", coords);
            var knife = SSpawnAtPosition("KitchenKnife", coords);

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(SEntMan.Deleted(knife) || SEntMan.IsQueuedForDeletion(knife), Is.True, "Нож не поглощён");
            Assert.That(heretics.CountLimitedResults(heretic, "STTestHereticRitualLimited"), Is.EqualTo(1));

            // Лимит 1: повтор не срабатывает.
            SSpawnAtPosition("KitchenKnife", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False);
        });
    }

    [Test]
    public async Task AshBladeRitual()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var heretic = new Entity<StoriesHereticComponent>(uid, comp);

            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeAshRoot"), Is.True);
            Assert.That(comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualAshBlade"));

            var rune = SSpawnAtPosition("STHereticRune", coords);
            SSpawnAtPosition("KitchenKnife", coords);
            SSpawnAtPosition("Lighter", coords);

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(heretics.CountLimitedResults(heretic, "STHereticRitualAshBlade"), Is.EqualTo(1));
        });
    }
}
