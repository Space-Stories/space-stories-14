#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Ghouls;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticGhoulTest : GameTest
{
    [Test]
    public async Task RaiseGhoulRules()
    {
        var testMap = await Pair.CreateTestMap();
        var ghouls = Server.System<StoriesHereticGhoulSystem>();
        var damageable = Server.System<DamageableSystem>();
        var mobState = Server.System<MobStateSystem>();
        var thresholds = Server.System<MobThresholdSystem>();
        var minds = Server.System<SharedMindSystem>();
        var master = EntityUid.Invalid;
        var corpse = EntityUid.Invalid;

        EntityUid Corpse()
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            minds.TransferTo(minds.CreateMind(null), uid);
            damageable.TryChangeDamage(uid, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            Assert.That(mobState.IsDead(uid));
            return uid;
        }

        await Server.WaitAssertion(() =>
        {
            master = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var alive = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            Assert.That(ghouls.TryRaise(master, alive, "STHereticGhoul", 1, out _), Is.False, "Поднят живой");

            corpse = Corpse();
            Assert.That(ghouls.TryRaise(master, corpse, "STHereticGhoul", 1, out _), Is.True);
            Assert.That(mobState.IsAlive(corpse), Is.True);
            Assert.That(thresholds.TryGetThresholdForState(corpse, MobState.Dead, out var dead) && dead == 50, "Пороги гуля не выставлены");
            Assert.That(ghouls.CountGhouls(master, "STHereticGhoul"), Is.EqualTo(1));

            Assert.That(ghouls.TryRaise(master, Corpse(), "STHereticGhoul", 1, out _), Is.False, "Лимит не сработал");

            // Смерть гуля освобождает слот, тело остаётся трупом.
            damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 100 } } }, true);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(mobState.IsDead(corpse), Is.True, "Снятие обращения оживило труп");
            Assert.That(ghouls.CountGhouls(master, "STHereticGhoul"), Is.Zero);
            Assert.That(ghouls.TryRaise(master, corpse, "STHereticGhoul", 1, out _), Is.False, "Тот же труп поднят дважды");
        });
    }
}
