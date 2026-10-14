#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Summons;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticSummonTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: [ MobHuman, STHereticSummonBase ]
  id: STTestHereticSummonMob
  components:
  - type: GhostRole
    name: test
    description: test
    rules: test
    mindRoles:
    - STMindRoleHereticMonster

- type: entity
  id: STTestHereticRitualSummon
  components:
  - type: StoriesHereticRitual
    effect: Summon
    ingredients:
    - name: stories-heretic-ingredient-knife
      whitelist:
        tags: [ Knife ]
  - type: StoriesHereticSummonRitual
    mob: STTestHereticSummonMob
";

    private int CountKnives()
    {
        var count = 0;
        var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var meta))
        {
            if (meta.EntityPrototype?.ID == "KitchenKnife" && !SEntMan.IsQueuedForDeletion(uid))
                count++;
        }

        return count;
    }

    [Test]
    public async Task UnclaimedSummonReturnsIngredients()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var timing = Server.ResolveDependency<IGameTiming>();
        var mob = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            comp.Rituals.Clear();
            comp.Rituals.Add("STTestHereticRitualSummon");
            var rune = SSpawnAtPosition("STHereticRune", coords);
            SSpawnAtPosition("KitchenKnife", coords);

            Assert.That(rituals.TryPerform((uid, comp), rune), Is.True);
            Assert.That(CountKnives(), Is.Zero, "Нож не поглощён");

            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent>();
            Assert.That(query.MoveNext(out mob, out var summon), Is.True, "Моб не призван");
            Assert.That(summon!.Master, Is.EqualTo(uid));
            summon.ExpireTime = timing.CurTime;
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(mob) || SEntMan.IsQueuedForDeletion(mob), Is.True, "Невостребованный призыв остался");
            Assert.That(CountKnives(), Is.EqualTo(1), "Ингредиенты не вернулись");
        });
    }

    [Test]
    public async Task ClaimedSummonCountsAndDropsOnDeath()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var minds = Server.System<SharedMindSystem>();
        var damageable = Server.System<DamageableSystem>();
        var heretic = default(Entity<StoriesHereticComponent>);
        var mob = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            heretic = (uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            heretic.Comp.Rituals.Clear();
            heretic.Comp.Rituals.Add("STTestHereticRitualSummon");
            var rune = SSpawnAtPosition("STHereticRune", coords);
            SSpawnAtPosition("KitchenKnife", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent>();
            query.MoveNext(out mob, out _);

            var mindId = minds.CreateMind(null);
            minds.TransferTo(mindId, mob);
            Assert.That(heretic.Comp.SummonCount, Is.EqualTo(1), "Призыв не засчитан");
            Assert.That(SEntMan.GetComponent<StoriesHereticSummonComponent>(mob).Bound, Is.True);

            damageable.TryChangeDamage(mob, new DamageSpecifier { DamageDict = new() { { "Blunt", 1000 } } }, true);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(mob) || SEntMan.IsQueuedForDeletion(mob), Is.True, "Мёртвый призыв не схлопнулся");
            Assert.That(CountKnives(), Is.EqualTo(1), "Призыв не выронил ингредиенты");
        });
    }
}
