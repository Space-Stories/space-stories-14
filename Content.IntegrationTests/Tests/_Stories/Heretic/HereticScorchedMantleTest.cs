#pragma warning disable RA0002
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Ash;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Body;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticScorchedMantleTest : GameTest
{
    private const string Mantle = "STHereticScorchedMantle";

    private static readonly string[] AshPathToVolcano =
    {
        "STHereticKnowledgeAshRoot", "STHereticKnowledgeAshGrasp", "STHereticKnowledgeAshPassage",
        "STHereticKnowledgeAshMark", "STHereticKnowledgeAshVolcano",
    };

    private Entity<StoriesHereticComponent> SpawnHeretic(EntityCoordinates coords)
    {
        var uid = SSpawnAtPosition("MobHuman", coords);
        return (uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
    }

    private EntityUid EquipMantle(EntityUid wearer)
    {
        var mantle = SSpawnAtPosition(Mantle, SEntMan.GetComponent<TransformComponent>(wearer).Coordinates);
        Assert.That(Server.System<InventorySystem>().TryEquip(wearer, mantle, "outerClothing", silent: true, force: true));
        return mantle;
    }

    private int CountMantles()
    {
        var count = 0;
        var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var meta))
        {
            if (meta.EntityPrototype?.ID == Mantle)
                count++;
        }

        return count;
    }

    [Test]
    public async Task RitualMakesMantle()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = SpawnHeretic(coords);
            heretics.AddPoints(heretic, 20);
            foreach (var id in AshPathToVolcano.Append("STHereticKnowledgeAshMantle"))
            {
                Assert.That(heretics.TryLearn(heretic, id), Is.True, id);
            }

            Assert.That(heretic.Comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualAshMantle"));

            var rune = SSpawnAtPosition("STHereticRune", coords);

            // Стол + маска + зажигалка.
            var table = SSpawnAtPosition("Table", coords);
            SSpawnAtPosition("ClothingMaskBreath", coords);
            SSpawnAtPosition("Lighter", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(SEntMan.Deleted(table) || SEntMan.IsQueuedForDeletion(table), Is.True, "Стол не поглощён");
            Assert.That(CountMantles(), Is.EqualTo(1));

            // Любая верхняя одежда вместо стола.
            SSpawnAtPosition("ClothingOuterRobesCult", coords);
            SSpawnAtPosition("ClothingMaskBreath", coords);
            SSpawnAtPosition("Lighter", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(CountMantles(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task HoodIsFocus()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var inventory = Server.System<InventorySystem>();

        await Server.WaitAssertion(() =>
        {
            var heretic = SpawnHeretic(testMap.GridCoords);
            var mantle = EquipMantle(heretic);
            Assert.That(heretics.HasFocus(heretic), Is.False, "Фокус с опущенным капюшоном");

            var hood = SEntMan.GetComponent<ToggleableClothingComponent>(mantle).ClothingUid!.Value;
            Assert.That(inventory.TryEquip(heretic, hood, "head", silent: true, force: true));
            Assert.That(heretics.HasFocus(heretic), Is.True, "Нет фокуса с поднятым капюшоном");
        });
    }

    [Test]
    public async Task MantleHidesAura()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var inventory = Server.System<InventorySystem>();

        await Server.WaitAssertion(() =>
        {
            var heretic = SpawnHeretic(testMap.GridCoords);
            heretics.AddPoints(heretic, heretic.Comp.PointsToAura + 1);
            Assert.That(heretic.Comp.AuraEntity, Is.Not.Null);

            EquipMantle(heretic);
            Assert.That(heretic.Comp.AuraEntity, Is.Null, "Аура видна в мантии");

            heretics.AddPoints(heretic, 1);
            Assert.That(heretic.Comp.AuraEntity, Is.Null, "Аура появилась в мантии");

            Assert.That(inventory.TryUnequip(heretic, "outerClothing", silent: true, force: true));
            Assert.That(heretic.Comp.AuraEntity, Is.Not.Null, "Аура не вернулась после снятия");
        });
    }

    [Test]
    public async Task MantleFlames()
    {
        var testMap = await Pair.CreateTestMap();
        var actions = Server.System<SharedActionsSystem>();
        var inventory = Server.System<InventorySystem>();

        await Server.WaitPost(() =>
        {
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;
            Server.System<AtmosphereSystem>().SetMapAtmosphere(testMap.MapUid, false, new GasMixture(moles, Atmospherics.T20C));
        });

        var heretic = default(Entity<StoriesHereticComponent>);
        var action = default(Entity<ActionComponent>);

        await Server.WaitAssertion(() =>
        {
            heretic = SpawnHeretic(testMap.GridCoords);
            var mantle = EquipMantle(heretic);
            var actionUid = SEntMan.GetComponent<ActionGrantComponent>(mantle).ActionEntities.Single();
            action = (actionUid, SEntMan.GetComponent<ActionComponent>(actionUid));
            Assert.That(actions.GetActions(heretic).Select(a => a.Owner), Does.Contain(actionUid), "Еретик не получил действие");
        });

        // Клиент должен увидеть еретика до поджога: FireVisualizerSystem апстрима создаёт огонёк прямо
        // в инициализации компонента, и моб, впервые пришедший клиенту горящим, роняет DebugAssert движка.
        await RunTicksSync(5);

        await Server.WaitAssertion(() =>
        {
            actions.PerformAction(heretic.Owner, action, predicted: false);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(heretic).OnFire, Is.True, "Носитель не загорелся");
        });

        await RunSeconds(6);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<FlammableComponent>(heretic).OnFire, Is.True, "Огонь затух");
            var damage = SEntMan.GetComponent<DamageableComponent>(heretic).Damage.DamageDict.GetValueOrDefault("Heat");
            Assert.That(damage.Float(), Is.LessThan(1f), "Мантия не защитила от огня");

            actions.PerformAction(heretic.Owner, action, predicted: false);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(heretic).OnFire, Is.False, "Выключение не потушило");

            // Не-еретику действие не выдаётся.
            var human = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var other = EquipMantle(human);
            var otherAction = SEntMan.GetComponent<ActionGrantComponent>(other).ActionEntities.Single();
            Assert.That(actions.GetActions(human).Select(a => a.Owner), Does.Not.Contain(otherAction));

            Assert.That(inventory.TryUnequip(heretic, "outerClothing", silent: true, force: true));
        });
    }

    [Test]
    public async Task DeletedMantleRestoresWearer()
    {
        var testMap = await Pair.CreateTestMap();
        var actions = Server.System<SharedActionsSystem>();

        var heretic = default(Entity<StoriesHereticComponent>);
        var fade = 0f;

        await Server.WaitAssertion(() =>
        {
            heretic = SpawnHeretic(testMap.GridCoords);
            fade = SEntMan.GetComponent<FlammableComponent>(heretic).FirestackFade;
            var mantle = EquipMantle(heretic);
            var actionUid = SEntMan.GetComponent<ActionGrantComponent>(mantle).ActionEntities.Single();
            actions.PerformAction(heretic.Owner, (actionUid, SEntMan.GetComponent<ActionComponent>(actionUid)), predicted: false);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(heretic).FirestackFade, Is.Zero);
            Assert.That(heretic.Comp.AuraHidden, Is.True);

            SEntMan.QueueDeleteEntity(mantle);
        });

        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<FlammableComponent>(heretic).FirestackFade, Is.EqualTo(fade), "Затухание огня не вернулось");
            Assert.That(heretic.Comp.AuraHidden, Is.False, "Аура осталась скрытой");
            Assert.That(SEntMan.GetComponent<FlammableComponent>(heretic).OnFire, Is.False, "Пламя мантии не погасло");

            // Компонент мантии снят без снятия одежды — срабатывает только страховка ComponentShutdown.
            var mantle = EquipMantle(heretic);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(heretic).FirestackFade, Is.Zero);
            SEntMan.RemoveComponent<StoriesHereticScorchedMantleComponent>(mantle);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(heretic).FirestackFade, Is.EqualTo(fade), "Страховка не вернула затухание");
            Assert.That(heretic.Comp.AuraHidden, Is.False, "Страховка не вернула ауру");
        });
    }

    [Test]
    public async Task NonHereticLosesLimbs()
    {
        var testMap = await Pair.CreateTestMap();
        var body = Server.System<BodySystem>();

        var victim = EntityUid.Invalid;
        var heretic = EntityUid.Invalid;
        var before = 0;

        int CountLimbs(EntityUid uid)
        {
            return body.EnumerateOrgans<DetachableOrganComponent>(uid)
                .Count(o => o.Comp1.Category is { } category
                            && (category == "ArmLeft" || category == "ArmRight"
                                                      || category == "LegLeft" || category == "LegRight"));
        }

        await Server.WaitAssertion(() =>
        {
            victim = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            heretic = SpawnHeretic(testMap.GridCoords);
            before = CountLimbs(victim);
            Assert.That(before, Is.EqualTo(4));
            EquipMantle(victim);
            EquipMantle(heretic);
        });

        await RunSeconds(2.5f);

        await Server.WaitAssertion(() =>
        {
            Assert.That(CountLimbs(victim), Is.LessThan(before), "Мантия не сожгла конечность не-еретика");
            Assert.That(CountLimbs(heretic), Is.EqualTo(4), "Мантия жжёт еретика");
        });
    }

    [Test]
    public async Task ArmorReducesPiercing()
    {
        var testMap = await Pair.CreateTestMap();
        var damageable = Server.System<DamageableSystem>();

        await Server.WaitAssertion(() =>
        {
            var hit = new DamageSpecifier { DamageDict = new() { { "Piercing", 10 } } };
            var bare = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var armored = SpawnHeretic(testMap.GridCoords);
            EquipMantle(armored);

            var bareDamage = damageable.ChangeDamage(bare, hit).GetTotal();
            var armoredDamage = damageable.ChangeDamage(armored.Owner, hit).GetTotal();
            Assert.That(armoredDamage.Float(), Is.LessThan(bareDamage.Float() * 0.5f));
        });
    }
}
