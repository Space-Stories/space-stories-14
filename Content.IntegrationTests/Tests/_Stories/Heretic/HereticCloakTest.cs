#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic.Cloak;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticCloakTest : GameTest
{
    [Test]
    public async Task CloakHidesAndBreaksOnDamage()
    {
        var testMap = await Pair.CreateTestMap();
        var damageable = Server.System<DamageableSystem>();
        var heretic = EntityUid.Invalid;
        var observer = EntityUid.Invalid;
        var realName = string.Empty;
        var baseSpeed = 0f;

        await Server.WaitAssertion(() =>
        {
            heretic = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            observer = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            SEntMan.AddComponent<StoriesHereticComponent>(heretic);
            realName = SEntMan.GetComponent<MetaDataComponent>(heretic).EntityName;
            baseSpeed = SEntMan.GetComponent<MovementSpeedModifierComponent>(heretic).CurrentSprintSpeed;

            var ev = new StoriesHereticShadowCloakEvent { Performer = heretic };
            SEntMan.EventBus.RaiseLocalEvent(heretic, ev);
            Assert.That(ev.Handled, Is.True);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<StoriesHereticShadowCloakComponent>(heretic));
            Assert.That(Identity.Name(heretic, SEntMan, observer), Is.Not.EqualTo(realName));
            Assert.That(SEntMan.GetComponent<MovementSpeedModifierComponent>(heretic).CurrentSprintSpeed, Is.GreaterThan(baseSpeed));

            damageable.TryChangeDamage(heretic, new DamageSpecifier { DamageDict = new() { { "Blunt", 30 } } }, true);
            damageable.TryChangeDamage(heretic, new DamageSpecifier { DamageDict = new() { { "Blunt", 70 } } }, true);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<StoriesHereticShadowCloakComponent>(heretic), Is.False, "Плащ не сорван уроном");
            Assert.That(Identity.Name(heretic, SEntMan, observer), Is.EqualTo(realName));
        });
    }
}
