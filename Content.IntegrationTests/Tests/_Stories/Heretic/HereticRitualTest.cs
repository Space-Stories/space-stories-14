#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
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

    [Test]
    public async Task AshAscensionOnce()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var damageable = Server.System<DamageableSystem>();
        var hands = Server.System<SharedHandsSystem>();
        var mobState = Server.System<MobStateSystem>();

        var heretic = default(Entity<StoriesHereticComponent>);
        var rune = EntityUid.Invalid;
        var knife = EntityUid.Invalid;

        EntityUid SpawnBurntCorpse()
        {
            var corpse = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Heat", 300 } } }, true);
            Assert.That(mobState.IsDead(corpse));
            return corpse;
        }

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            heretics.AddPoints(heretic, 20);

            foreach (var id in new[]
                     {
                         "STHereticKnowledgeAshRoot", "STHereticKnowledgeAshGrasp", "STHereticKnowledgeAshPassage",
                         "STHereticKnowledgeAshMark", "STHereticKnowledgeAshVolcano", "STHereticKnowledgeAshFieryBlade",
                         "STHereticKnowledgeAshRebirth",
                     })
            {
                Assert.That(heretics.TryLearn(heretic, id), Is.True, id);
            }

            heretic.Comp.SacrificeCount = heretic.Comp.RequiredSacrifices;
            heretic.Comp.HeadSacrificeCount = heretic.Comp.RequiredHeadSacrifices;
            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeAshAscension"), Is.True);

            rune = SSpawnAtPosition("STHereticRune", coords);
            var first = SSpawnAtPosition("MobHuman", coords);
            knife = SSpawnAtPosition("KitchenKnife", coords);
            Assert.That(hands.TryPickupAnyHand(first, knife), Is.True);
            damageable.TryChangeDamage(first, new DamageSpecifier { DamageDict = new() { { "Heat", 300 } } }, true);
            SpawnBurntCorpse();
            SpawnBurntCorpse();

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(heretic.Comp.Ascended, Is.True);
            Assert.That(heretic.Comp.Rituals, Does.Not.Contain((EntProtoId) "STHereticRitualAshAscension"));
            Assert.That(SEntMan.Deleted(knife) || SEntMan.IsQueuedForDeletion(knife), Is.False, "Вещи трупа уничтожены");
        });

        await Pair.RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            for (var i = 0; i < 3; i++)
            {
                SpawnBurntCorpse();
            }

            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "Вознесение повторилось");
        });
    }
}
