#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Ash;
using Content.Server.Body.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Explosion.Components;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Fluids;
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

- type: entity
  id: STTestHereticRitualArm
  components:
  - type: StoriesHereticRitual
    ingredients:
    - name: stories-heretic-ingredient-arm
      organCategory: ArmLeft
    results:
    - STTestHereticResult

- type: entity
  id: STTestHereticRitualEyes
  components:
  - type: StoriesHereticRitual
    ingredients:
    - name: stories-heretic-ingredient-eyes
      organCategory: Eyes
    results:
    - STTestHereticResult

- type: entity
  id: STTestHereticRitualBlood
  components:
  - type: StoriesHereticRitual
    ingredients:
    - name: stories-heretic-ingredient-blood
      reagents: [ Blood ]
    results:
    - STTestHereticResult
";

    [Test]
    public async Task BloodPuddleIngredient()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var puddles = Server.System<SharedPuddleSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            comp.Rituals.Clear();
            comp.Rituals.Add("STTestHereticRitualBlood");
            var heretic = new Entity<StoriesHereticComponent>(uid, comp);
            var rune = SSpawnAtPosition("STHereticRune", coords);

            // 5 ед. мало.
            Assert.That(puddles.TrySpillAt(coords, new Solution("Blood", 5), out _, sound: false), Is.True);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False);

            // Лужа на тайле сливается в одну: 5 + 10 = 15 ед.
            Assert.That(puddles.TrySpillAt(coords, new Solution("Blood", 10), out var puddle, sound: false), Is.True);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(SEntMan.Deleted(puddle) || SEntMan.IsQueuedForDeletion(puddle), Is.True, "Лужа не поглощена");
        });
    }

    [Test]
    public async Task DetachedLimbIsOrganIngredient()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var dismember = Server.System<Content.Server._Stories.Heretic.StoriesHereticDismemberSystem>();
        var containers = Server.System<Robust.Shared.Containers.SharedContainerSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            comp.Rituals.Clear();
            comp.Rituals.Add("STTestHereticRitualArm");
            var rune = SSpawnAtPosition("STHereticRune", coords);

            var corpse = SSpawnAtPosition("MobHuman", coords);
            Server.System<DamageableSystem>().TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            Assert.That(rituals.TryPerform((uid, comp), rune), Is.False, "Целое тело засчитано как рука");

            var arm = containers.GetContainer(corpse, Content.Shared.Body.BodyComponent.ContainerID).ContainedEntities
                .First(o => SEntMan.GetComponent<Content.Shared.Body.OrganComponent>(o).Category == "ArmLeft");
            var detached = dismember.Dismember(corpse, arm);
            Assert.That(detached, Is.Not.Null);

            Assert.That(rituals.TryPerform((uid, comp), rune), Is.True, "Отделённая рука не засчитана");
            Assert.That(SEntMan.Deleted(detached!.Value) || SEntMan.IsQueuedForDeletion(detached.Value), Is.True, "Рука не поглощена");
        });
    }

    [Test]
    public async Task DetachedHeadIsNotEyes()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var dismember = Server.System<Content.Server._Stories.Heretic.StoriesHereticDismemberSystem>();
        var containers = Server.System<Robust.Shared.Containers.SharedContainerSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            comp.Rituals.Clear();
            comp.Rituals.Add("STTestHereticRitualEyes");
            var rune = SSpawnAtPosition("STHereticRune", coords);

            var corpse = SSpawnAtPosition("MobHuman", coords);
            Server.System<DamageableSystem>().TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            var head = containers.GetContainer(corpse, Content.Shared.Body.BodyComponent.ContainerID).ContainedEntities
                .First(o => SEntMan.GetComponent<Content.Shared.Body.OrganComponent>(o).Category == "Head");
            Assert.That(dismember.Dismember(corpse, head), Is.Not.Null);

            Assert.That(rituals.TryPerform((uid, comp), rune), Is.False, "Отделённая голова засчитана как глаза");
        });
    }

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
    public async Task QueuedIngredientIsNotReused()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeAshRoot"), Is.True);

            var rune = SSpawnAtPosition("STHereticRune", coords);
            // Зажигалка одна (сам клинок — тоже нож, поэтому ножей хватает на второй раз).
            SSpawnAtPosition("KitchenKnife", coords);
            SSpawnAtPosition("KitchenKnife", coords);
            SSpawnAtPosition("Lighter", coords);

            // Зажигалка поглощена первым ритуалом и ждёт удаления до конца тика — второй её не видит.
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "Зажигалка из очереди на удаление использована снова");
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

            // Как ash_final в tg: вдвое больше скачков Извержения, короче перезарядки, иммунитеты.
            var volcano = FindAction(heretic, "STActionHereticVolcanoBlast");
            var volcanoEvent = (StoriesHereticVolcanoBlastEvent) SEntMan.GetComponent<InstantActionComponent>(volcano).Event!;
            Assert.That(volcanoEvent.Bounces, Is.EqualTo(10));
            Assert.That(volcano.Comp.UseDelay, Is.EqualTo(TimeSpan.FromSeconds(30) * 0.66f));
            Assert.That(FindAction(heretic, "STActionHereticRebirth").Comp.UseDelay, Is.EqualTo(TimeSpan.FromSeconds(60) * 0.16f));
            Assert.That(SEntMan.GetComponent<ExplosionResistanceComponent>(heretic).DamageCoefficient, Is.Zero);
            Assert.That(SEntMan.HasComponent<RespiratorComponent>(heretic), Is.False, "Вознёсшийся всё ещё дышит");

            // Усиление не задело данные прототипа: новое действие с обычным числом скачков.
            var fresh = SSpawn("STActionHereticVolcanoBlast");
            Assert.That(((StoriesHereticVolcanoBlastEvent) SEntMan.GetComponent<InstantActionComponent>(fresh).Event!).Bounces, Is.EqualTo(5));
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

    private Entity<ActionComponent> FindAction(EntityUid holder, string proto)
    {
        foreach (var action in Server.System<SharedActionsSystem>().GetActions(holder))
        {
            if (SEntMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID == proto)
                return action;
        }

        Assert.Fail($"Нет действия {proto}");
        return default;
    }

    [Test]
    public async Task FireRingFollowsAndExpires()
    {
        var testMap = await Pair.CreateTestMap();
        var heretic = EntityUid.Invalid;
        var ring = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            heretic = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            SEntMan.AddComponent<StoriesHereticComponent>(heretic);
            SEntMan.EventBus.RaiseLocalEvent(heretic, new StoriesHereticFireRingEvent { Duration = TimeSpan.FromSeconds(1) });
            SEntMan.EventBus.RaiseLocalEvent(heretic, new StoriesHereticFireRingEvent { Duration = TimeSpan.FromSeconds(1) });

            ring = SEntMan.GetComponent<StoriesHereticFireRingComponent>(heretic).RingEntity!.Value;
            Assert.That(SEntMan.GetComponent<TransformComponent>(ring).ParentUid, Is.EqualTo(heretic));
            var rings = 0;
            var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out var meta))
            {
                if (meta.EntityPrototype?.ID == "STHereticFireRingEffect")
                    rings++;
            }

            Assert.That(rings, Is.EqualTo(1), "Повторный каст продублировал кольцо");
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<StoriesHereticFireRingComponent>(heretic), Is.False);
            Assert.That(SEntMan.Deleted(ring), Is.True, "Кольцо осталось после окончания");
        });
    }
}
