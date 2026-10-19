#pragma warning disable RA0002
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticShadowRealmMapTest : GameTest
{
    private static readonly ResPath RealmMap = new("/Maps/_Stories/Nonstations/heretic_shadow_realm.yml");

    public override PoolSettings PoolSettings => new() { Connected = true, Fresh = true, Dirty = true };

    [Test]
    public async Task VictimLandsOnRealmSpawn()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var damageable = Server.System<DamageableSystem>();

        await Server.WaitAssertion(() =>
        {
            var ruleEnt = SSpawn(null);
            SEntMan.AddComponent<StoriesHereticRuleComponent>(ruleEnt).ShadowRealmMap = RealmMap;

            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            var rune = SSpawnAtPosition("STHereticRune", coords);
            var victim = SSpawnAtPosition("MobHuman", coords);
            damageable.TryChangeDamage(victim, new DamageSpecifier { DamageDict = new() { { "Blunt", 300 } } }, true);

            heretics.SetTargets(heretic, new() { victim });
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            var xform = SEntMan.GetComponent<TransformComponent>(victim);
            Assert.That(xform.MapID, Is.Not.EqualTo(testMap.MapId), "Жертва не перенесена в Теневой мир");
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(victim).EntityPaused, Is.False, "Карта Теневого мира на паузе");

            var spawns = new List<EntityCoordinates>();
            var query = SEntMan.EntityQueryEnumerator<StoriesHereticShadowRealmSpawnComponent, TransformComponent>();
            while (query.MoveNext(out _, out _, out var spawnXform))
            {
                if (spawnXform.GridUid == xform.GridUid)
                    spawns.Add(spawnXform.Coordinates);
            }

            Assert.That(spawns, Has.Count.EqualTo(4), "На карте должно быть 4 точки появления");
            Assert.That(spawns.Any(s => (s.Position - xform.Coordinates.Position).Length() < 0.01f),
                "Жертва появилась не на точке появления");

            var mapUid = xform.MapUid!.Value;
            Assert.That(SEntMan.HasComponent<MapLightComponent>(mapUid), "Нет освещения карты");
            Assert.That(SEntMan.GetComponent<GravityComponent>(mapUid).Enabled, "Нет гравитации в Теневом мире");
        });
    }
}
