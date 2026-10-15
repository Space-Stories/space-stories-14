#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Ghouls;
using Content.Shared._Stories.Heretic;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

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
        var loc = Server.ResolveDependency<ILocalizationManager>();
        var heretics = Server.System<StoriesHereticSystem>();
        var master = EntityUid.Invalid;
        var corpse = EntityUid.Invalid;
        var alive = EntityUid.Invalid;
        var ghoulMind = EntityUid.Invalid;

        // Душа — разум игрока. Игрок в тесте один: каждый новый труп забирает его разум себе.
        EntityUid Corpse(bool soul = true)
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            minds.TransferTo(minds.CreateMind(soul ? ServerSession!.UserId : null), uid);
            damageable.TryChangeDamage(uid, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            Assert.That(mobState.IsDead(uid));
            return uid;
        }

        await Server.WaitAssertion(() =>
        {
            master = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            alive = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            Assert.That(ghouls.TryRaise(master, alive, "STHereticGhoul", 1, out _), Is.False, "Поднят живой");

            Assert.That(ghouls.TryRaise(master, Corpse(soul: false), "STHereticGhoul", 1, out var reason), Is.False, "Поднято тело без души");
            Assert.That(reason, Is.EqualTo(loc.GetString("stories-heretic-ghoul-no-soul")));

            corpse = Corpse();
            Assert.That(ghouls.TryRaise(master, corpse, "STHereticGhoul", 1, out _), Is.True);
            Assert.That(mobState.IsAlive(corpse), Is.True);
            Assert.That(thresholds.TryGetThresholdForState(corpse, MobState.Dead, out var dead) && dead == 50, "Пороги гуля не выставлены");
            Assert.That(ghouls.CountGhouls(master, "STHereticGhoul"), Is.EqualTo(1));
            Assert.That(heretics.IsServant(corpse, master), Is.True, "Гуль не слуга хозяина");
            Assert.That(minds.TryGetMind(corpse, out ghoulMind, out _), Is.True);
            Assert.That(minds.TryFindObjective(ghoulMind, "STHereticObjectiveServeMaster", out _), Is.True, "Нет цели служения");
            Assert.That(heretics.IsServant(alive), Is.False, "Случайный моб — слуга");

            Assert.That(ghouls.TryRaise(master, Corpse(), "STHereticGhoul", 1, out reason), Is.False, "Лимит не сработал");
            Assert.That(reason, Is.EqualTo(loc.GetString("stories-heretic-ghoul-limit")));

            // Смерть гуля освобождает слот, тело остаётся трупом.
            damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 100 } } }, true);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(mobState.IsDead(corpse), Is.True, "Снятие обращения оживило труп");
            Assert.That(ghouls.CountGhouls(master, "STHereticGhoul"), Is.Zero);
            Assert.That(heretics.IsServant(corpse), Is.False, "Мёртвый гуль остался слугой");
            Assert.That(minds.TryFindObjective(ghoulMind, "STHereticObjectiveServeMaster", out _), Is.False, "Цель служения осталась после смерти гуля");
            Assert.That(ghouls.TryRaise(master, corpse, "STHereticGhoul", 1, out var again), Is.False, "Тот же труп поднят дважды");
            Assert.That(again, Is.EqualTo(loc.GetString("stories-heretic-ghoul-already")));

            // Оживлённый бывший гуль снова обычной прочности.
            Server.System<RejuvenateSystem>().PerformRejuvenate(corpse);
            Assert.That(mobState.IsAlive(corpse), Is.True);
            Assert.That(thresholds.TryGetThresholdForState(corpse, MobState.Critical, out var crit) && crit == 100, "Порог крита не вернулся");
            Assert.That(thresholds.TryGetThresholdForState(corpse, MobState.Dead, out var dead) && dead == 200, "Порог смерти не вернулся");
        });
    }

    [Test]
    public async Task GibbedGhoulLosesServeObjective()
    {
        var testMap = await Pair.CreateTestMap();
        var ghouls = Server.System<StoriesHereticGhoulSystem>();
        var damageable = Server.System<DamageableSystem>();
        var minds = Server.System<SharedMindSystem>();
        var mindId = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            var master = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var corpse = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            mindId = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mindId, corpse);
            damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            Assert.That(ghouls.TryRaise(master, corpse, "STHereticGhoul", 1, out _), Is.True);
            Assert.That(minds.TryFindObjective(mindId, "STHereticObjectiveServeMaster", out _), Is.True);

            // Гуля разорвало: тело удаляется, разум уходит в призрака.
            Server.System<GibbingSystem>().Gib(corpse);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(mindId), Is.True, "Разум пропал вместе с телом");
            Assert.That(minds.TryFindObjective(mindId, "STHereticObjectiveServeMaster", out _), Is.False, "Цель служения осталась после разрыва гуля");
        });
    }

    [Test]
    public async Task ServeObjectiveReturnsWithMind()
    {
        var testMap = await Pair.CreateTestMap();
        var ghouls = Server.System<StoriesHereticGhoulSystem>();
        var damageable = Server.System<DamageableSystem>();
        var minds = Server.System<SharedMindSystem>();

        await Server.WaitAssertion(() =>
        {
            var master = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var ghoul = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var elsewhere = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var mindId = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mindId, ghoul);
            damageable.TryChangeDamage(ghoul, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            Assert.That(ghouls.TryRaise(master, ghoul, "STHereticGhoul", 1, out _), Is.True);

            // Разум временно ушёл из живого гуля (полиморф, админ-управление мобом) и вернулся.
            minds.TransferTo(mindId, elsewhere);
            Assert.That(minds.TryFindObjective(mindId, "STHereticObjectiveServeMaster", out _), Is.False, "Цель осталась вне тела гуля");
            minds.TransferTo(mindId, ghoul);
            Assert.That(minds.TryFindObjective(mindId, "STHereticObjectiveServeMaster", out _), Is.True, "Цель не вернулась вместе с разумом");
            Assert.That(SEntMan.GetComponent<MindComponent>(mindId).Objectives
                .Count(o => SEntMan.GetComponent<MetaDataComponent>(o).EntityPrototype?.ID == "STHereticObjectiveServeMaster"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task RaiseReturnsGhostedPlayer()
    {
        var testMap = await Pair.CreateTestMap();
        var ghouls = Server.System<StoriesHereticGhoulSystem>();
        var damageable = Server.System<DamageableSystem>();
        var minds = Server.System<SharedMindSystem>();

        await Server.WaitAssertion(() =>
        {
            var master = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var corpse = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var mind = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mind, corpse);
            damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);

            // Игрок вышел из мёртвого тела призраком, но может вернуться: разум лишь «навещает» призрака.
            var ghost = SSpawnAtPosition("MobObserver", testMap.GridCoords);
            minds.Visit(mind, ghost);
            Assert.That(ServerSession.AttachedEntity, Is.EqualTo(ghost));

            Assert.That(ghouls.TryRaise(master, corpse, "STHereticGhoul", 1, out _), Is.True);
            Assert.That(mind.Comp.VisitingEntity, Is.Null, "Разум остался в призраке");
            Assert.That(ServerSession.AttachedEntity, Is.EqualTo(corpse), "Игрок не вернулся в тело");
        });
    }
}
