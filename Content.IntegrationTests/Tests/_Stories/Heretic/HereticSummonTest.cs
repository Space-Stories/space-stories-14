#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Summons;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Damage;
using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Stacks;
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

- type: entity
  id: STTestHereticRitualSummonStack
  components:
  - type: StoriesHereticRitual
    effect: Summon
    ingredients:
    - name: stories-heretic-ingredient-glass
      prototypes: [ SheetGlass ]
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
    public async Task DirectlySpawnedSummonSurvivesThenExpires()
    {
        var testMap = await Pair.CreateTestMap();
        var timing = Server.ResolveDependency<IGameTiming>();
        var mob = EntityUid.Invalid;

        await Server.WaitAssertion(() => mob = SSpawnAtPosition("STTestHereticSummonMob", testMap.GridCoords));
        await Pair.RunTicksSync(5);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(mob) || SEntMan.IsQueuedForDeletion(mob), Is.False, "Моб схлопнулся сразу");
            SEntMan.GetComponent<StoriesHereticSummonComponent>(mob).ExpireTime = timing.CurTime;
        });
        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.Deleted(mob) || SEntMan.IsQueuedForDeletion(mob), Is.True, "Моб не схлопнулся по сроку"));
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
        var rune = EntityUid.Invalid;
        var mindId = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            heretic = (uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            heretic.Comp.Rituals.Clear();
            heretic.Comp.Rituals.Add("STTestHereticRitualSummon");
            rune = SSpawnAtPosition("STHereticRune", coords);
            SSpawnAtPosition("KitchenKnife", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);

            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent>();
            query.MoveNext(out mob, out _);
            Assert.That(SEntMan.HasComponent<StoriesHereticSummonWaitingComponent>(mob), Is.True, "Нет компонента ожидания");

            mindId = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mindId, mob);
            Assert.That(SEntMan.HasComponent<StoriesHereticSummonWaitingComponent>(mob), Is.False, "Ожидание не снято");
            Assert.That(SEntMan.GetComponent<MindComponent>(mindId).Objectives.Select(o => SEntMan.GetComponent<MetaDataComponent>(o).EntityPrototype?.ID),
                Does.Contain("STHereticObjectiveServeMaster"), "Нет цели слуги");
            Assert.That(heretic.Comp.SummonCount, Is.EqualTo(1), "Призыв не засчитан");
            Assert.That(Server.System<StoriesHereticSystem>().IsServant(mob, heretic), Is.True, "Призыв не слуга хозяина");
            Assert.That(SEntMan.GetComponent<StoriesHereticSummonComponent>(mob).Bound, Is.True);
        });

        // Поглощённый нож удаляется в конце тика — иначе второй ритуал нашёл бы его снова.
        await Pair.RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            // Тот же игрок во втором призыве цель не продвигает.
            SSpawnAtPosition("KitchenKnife", testMap.GridCoords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            var second = EntityUid.Invalid;
            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent>();
            while (query.MoveNext(out var uid2, out _))
            {
                if (uid2 != mob)
                    second = uid2;
            }

            minds.TransferTo(mindId, second);
            Assert.That(SEntMan.GetComponent<StoriesHereticSummonComponent>(second).Bound, Is.True);
            Assert.That(heretic.Comp.SummonCount, Is.EqualTo(1), "Один игрок засчитан дважды");

            damageable.TryChangeDamage(mob, new DamageSpecifier { DamageDict = new() { { "Blunt", 1000 } } }, true);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(mob) || SEntMan.IsQueuedForDeletion(mob), Is.True, "Мёртвый призыв не схлопнулся");
            Assert.That(CountKnives(), Is.EqualTo(1), "Призыв не выронил ингредиенты");
        });
    }

    [Test]
    public async Task StackIngredientReturnsOnlyOneUnit()
    {
        var testMap = await Pair.CreateTestMap();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var timing = Server.ResolveDependency<IGameTiming>();
        var original = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            comp.Rituals.Clear();
            comp.Rituals.Add("STTestHereticRitualSummonStack");
            var rune = SSpawnAtPosition("STHereticRune", coords);
            original = SSpawnAtPosition("SheetGlass", coords);
            var stack = SEntMan.GetComponent<StackComponent>(original);
            Assert.That(stack.Count, Is.GreaterThan(1));

            Assert.That(rituals.TryPerform((uid, comp), rune), Is.True);
            Assert.That(SEntMan.GetComponent<StackComponent>(original).Count, Is.EqualTo(stack.Count));

            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent>();
            Assert.That(query.MoveNext(out _, out var summon), Is.True);
            summon.ExpireTime = timing.CurTime;
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            var total = 0;
            var query = SEntMan.AllEntityQueryEnumerator<StackComponent>();
            while (query.MoveNext(out var uid, out var stack))
            {
                if (stack.StackTypeId == "Glass" && !SEntMan.IsQueuedForDeletion(uid))
                    total += stack.Count;
            }

            var originalCount = SEntMan.GetComponent<StackComponent>(original).Count;
            Assert.That(originalCount, Is.EqualTo(29), "стопка на руне");
            Assert.That(total, Is.EqualTo(30), "стекло продублировано или потеряно");
        });
    }
}
