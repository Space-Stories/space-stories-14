#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticSacrificeTest : GameTest
{
    [Test]
    public async Task SacrificeTarget()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var damageable = Server.System<DamageableSystem>();
        var mobState = Server.System<MobStateSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var heretic = new Entity<StoriesHereticComponent>(uid, comp);
            var points = comp.Points;

            var rune = SSpawnAtPosition("STHereticRune", coords);
            var stranger = SSpawnAtPosition("MobHuman", coords);
            damageable.TryChangeDamage(stranger, new DamageSpecifier { DamageDict = new() { { "Blunt", 300 } } }, true);
            Assert.That(mobState.IsDead(stranger));

            // Не цель — не принимается.
            heretics.SetTargets(heretic, new());
            Assert.That(rituals.TryPerform(heretic, rune), Is.False);

            // Цель — принимается.
            heretics.SetTargets(heretic, new() { stranger });
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(comp.SacrificeCount, Is.EqualTo(1));
            Assert.That(comp.Points, Is.EqualTo(points + comp.SacrificePoints));
            Assert.That(SEntMan.HasComponent<StoriesHereticSacrificedComponent>(stranger));
            Assert.That(mobState.IsAlive(stranger), "Жертва не исцелена");
            Assert.That(SEntMan.GetComponent<TransformComponent>(stranger).MapID, Is.Not.EqualTo(testMap.MapId),
                "Жертва не перенесена в Теневой мир");
        });
    }
}
