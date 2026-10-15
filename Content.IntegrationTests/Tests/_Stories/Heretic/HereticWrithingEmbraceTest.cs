#pragma warning disable RA0002
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Flesh;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Fluids;
using Content.Shared.Inventory;
using Content.Shared.Overlays;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticWrithingEmbraceTest : GameTest
{
    private const string Embrace = "STHereticWrithingEmbrace";

    private Entity<StoriesHereticComponent> SpawnHeretic(EntityCoordinates coords)
    {
        var uid = SSpawnAtPosition("MobHuman", coords);
        return (uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
    }

    /// <summary>
    /// Воздух на всей карте: в вакууме баротравма портит счёт урона.
    /// </summary>
    private async Task Breathable(EntityUid map)
    {
        await Server.WaitPost(() =>
        {
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;
            Server.System<AtmosphereSystem>().SetMapAtmosphere(map, false, new GasMixture(moles, Atmospherics.T20C));
        });
    }

    private EntityUid EquipEmbrace(EntityUid wearer)
    {
        var embrace = SSpawnAtPosition(Embrace, SEntMan.GetComponent<TransformComponent>(wearer).Coordinates);
        Assert.That(Server.System<InventorySystem>().TryEquip(wearer, embrace, "outerClothing", silent: true, force: true));
        return embrace;
    }

    private EntityUid SpawnServant(EntityCoordinates coords, EntityUid master)
    {
        var uid = SSpawnAtPosition("MobHuman", coords);
        SEntMan.AddComponent<StoriesHereticServantComponent>(uid).Master = master;
        return uid;
    }

    private float Brute(EntityUid uid)
    {
        var dict = SEntMan.GetComponent<DamageableComponent>(uid).Damage.DamageDict;
        return (dict.GetValueOrDefault("Blunt") + dict.GetValueOrDefault("Slash") + dict.GetValueOrDefault("Piercing")).Float();
    }

    [Test]
    public async Task RitualMakesEmbrace()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var puddles = Server.System<SharedPuddleSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var heretic = SpawnHeretic(coords);
            heretic.Comp.Points = 20;
            foreach (var id in new[] { "Root", "Grasp", "Imperfect", "Mark", "Knitting", "Mantle" })
            {
                Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFlesh" + id), Is.True, id);
            }

            Assert.That(heretic.Comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualFleshMantle"));

            var rune = SSpawnAtPosition("STHereticRune", coords);
            SSpawnAtPosition("Table", coords);
            SSpawnAtPosition("ClothingMaskBreath", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False, "Ритуал сработал без крови");

            Assert.That(puddles.TrySpillAt(coords, new Solution("Blood", 10), out _, sound: false), Is.True);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            var found = false;
            var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out var meta))
            {
                found |= meta.EntityPrototype?.ID == Embrace;
            }

            Assert.That(found, Is.True, "Мантия не создана");
        });
    }

    [Test]
    public async Task HoodGivesFocusAndMedicalHud()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var inventory = Server.System<InventorySystem>();

        await Server.WaitAssertion(() =>
        {
            var heretic = SpawnHeretic(testMap.GridCoords);
            var embrace = EquipEmbrace(heretic);
            Assert.That(heretics.HasFocus(heretic), Is.False, "Фокус с опущенным капюшоном");
            Assert.That(heretic.Comp.AuraHidden, Is.True, "Аура не скрыта");

            var hood = SEntMan.GetComponent<ToggleableClothingComponent>(embrace).ClothingUid!.Value;
            Assert.That(SEntMan.HasComponent<ShowHealthBarsComponent>(hood), Is.True, "Нет медицинского HUD");
            Assert.That(inventory.TryEquip(heretic, hood, "head", silent: true, force: true));
            Assert.That(heretics.HasFocus(heretic), Is.True, "Нет фокуса с поднятым капюшоном");

            Assert.That(inventory.TryUnequip(heretic, "outerClothing", silent: true, force: true));
            Assert.That(heretic.Comp.AuraHidden, Is.False, "Аура не вернулась после снятия");
        });
    }

    [Test]
    public async Task AuraHealsOwnServantsNearby()
    {
        var testMap = await Pair.CreateTestMap();
        var damageable = Server.System<DamageableSystem>();
        var hit = new DamageSpecifier { DamageDict = new() { { "Blunt", 30 } } };
        await Breathable(testMap.MapUid);

        EntityUid heretic = default, near = default, far = default, stranger = default;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            heretic = SpawnHeretic(coords);
            near = SpawnServant(coords, heretic);
            far = SpawnServant(coords.Offset(new System.Numerics.Vector2(30, 0)), heretic);
            stranger = SpawnServant(coords, SSpawnAtPosition("MobHuman", coords));
            foreach (var uid in new[] { heretic, near, far, stranger })
            {
                damageable.TryChangeDamage(uid, hit, true);
            }

            EquipEmbrace(heretic);
        });

        await RunSeconds(3.5f);

        await Server.WaitAssertion(() =>
        {
            Assert.That(Brute(near), Is.LessThan(25f), "Слуга рядом не лечится");
            Assert.That(Brute(far), Is.EqualTo(30f), "Лечится слуга вне радиуса");
            Assert.That(Brute(stranger), Is.EqualTo(30f), "Лечится чужой слуга");
            Assert.That(Brute(heretic), Is.EqualTo(30f), "Мантия лечит самого носителя");
        });
    }

    [Test]
    public async Task NonHereticGetsWounded()
    {
        var testMap = await Pair.CreateTestMap();
        var embraceRef = default(EntityUid);
        var victim = default(EntityUid);
        await Breathable(testMap.MapUid);

        await Server.WaitAssertion(() =>
        {
            victim = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            embraceRef = EquipEmbrace(victim);
        });

        await RunSeconds(8);

        await Server.WaitAssertion(() =>
        {
            var comp = SEntMan.GetComponent<StoriesHereticWrithingEmbraceComponent>(embraceRef);
            // Шесть ран мимо брони: по 10 резаных за каждую.
            Assert.That(SEntMan.GetComponent<DamageableComponent>(victim).Damage.DamageDict.GetValueOrDefault("Slash").Float(),
                Is.EqualTo(comp.Wound.DamageDict["Slash"].Float() * comp.Wounds).Within(0.01f));
            Assert.That(SEntMan.GetComponent<BloodstreamComponent>(victim).BleedAmount, Is.GreaterThan(0f), "Нет кровотечения");
            Assert.That(comp.WoundsLeft, Is.Zero);
        });
    }
}
