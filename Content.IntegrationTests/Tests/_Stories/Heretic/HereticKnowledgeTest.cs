#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticKnowledgeTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: Tag
  id: STTestHereticPathA

- type: Tag
  id: STTestHereticPathB

- type: entity
  id: STTestHereticKnowledgeStart
  components:
  - type: StoriesHereticKnowledge
    starting: true
    cost: 0

- type: entity
  id: STTestHereticKnowledgeRootA
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathA
    choosesPath: true
    stage: 1

- type: entity
  id: STTestHereticKnowledgeRootB
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathB
    choosesPath: true
    stage: 1

- type: entity
  id: STTestHereticKnowledgeA2
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathA
    stage: 2
    requires: [ STTestHereticKnowledgeRootA ]

- type: entity
  id: STTestHereticKnowledgeAscend
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathA
    stage: 3
    cost: 0
    ascension: true

- type: entity
  id: STTestHereticKnowledgeFeast
  components:
  - type: StoriesHereticKnowledge
    cost: 0
    grantPoints: 5
    locksAscension: true

- type: entity
  id: STTestHereticKnowledgeGraspB
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathB
    stage: 2
    pathLocked: true
    requires: [ STTestHereticKnowledgeRootB ]

- type: entity
  id: STTestHereticKnowledgeBridgeAB
  components:
  - type: StoriesHereticKnowledge
    stage: 3
    bridge: [ STTestHereticPathA, STTestHereticPathB ]
    requires: [ STTestHereticKnowledgeA2 ]

- type: entity
  id: STTestHereticKnowledgeB3
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathB
    stage: 3
    requires: [ STTestHereticKnowledgeGraspB, STTestHereticKnowledgeBridgeAB ]
";

    [Test]
    public async Task LearningRules()
    {
        var testMap = await Pair.CreateTestMap();
        var sys = Server.System<StoriesHereticSystem>();

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var ent = new Entity<StoriesHereticComponent>(uid, comp);

            // Стартовое знание выдано бесплатно и не считается купленным.
            Assert.That(comp.Knowledge, Does.Contain((EntProtoId) "STTestHereticKnowledgeStart"));
            Assert.That(comp.LearnedCount, Is.Zero);
            Assert.That(comp.Points, Is.EqualTo(1));

            // Без корня пути следующая ступень недоступна.
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeA2"), Is.False);

            // Выбор пути списывает очко и фиксирует путь.
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeRootA"), Is.True);
            Assert.That(comp.Points, Is.Zero);
            Assert.That(comp.Path?.Id, Is.EqualTo("STTestHereticPathA"));
            Assert.That(comp.LearnedCount, Is.EqualTo(1));

            // Второй корень закрыт.
            sys.AddPoints(ent, 5);
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeRootB"), Is.False);

            // Следующая ступень своего пути доступна.
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeA2"), Is.True);

            // Вознесение без выполненных целей недоступно.
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeAscend"), Is.False);
        });
    }

    [Test]
    public async Task CrossPathThroughBridge()
    {
        var testMap = await Pair.CreateTestMap();
        var sys = Server.System<StoriesHereticSystem>();

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var ent = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            sys.AddPoints(ent, 10);

            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeRootA"), Is.True);
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeA2"), Is.True);

            // Закрытое знание чужого пути нельзя даже при выполненных требованиях.
            Assert.That(sys.CanLearn(ent, "STTestHereticKnowledgeGraspB", out _), Is.False);

            // Без моста узел чужого пути закрыт требованиями.
            Assert.That(sys.CanLearn(ent, "STTestHereticKnowledgeB3", out _), Is.False);

            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeBridgeAB"), Is.True);
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeB3"), Is.True, "Мост не открыл знание соседнего пути");
            Assert.That(ent.Comp.Path, Is.EqualTo((ProtoId<TagPrototype>) "STTestHereticPathA"), "Переход сменил путь");
        });
    }

    [Test]
    public async Task LearnMessageOnlyFromHeretic()
    {
        var testMap = await Pair.CreateTestMap();
        var actions = Server.System<SharedActionsSystem>();
        var ui = Server.System<SharedUserInterfaceSystem>();
        var holder = EntityUid.Invalid;
        var before = 0;
        var beforeEntities = 0;

        int ActionEntities(EntityUid uid) =>
            SEntMan.GetComponent<ActionsContainerComponent>(uid).Container.ContainedEntities.Count;

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            holder = uid;
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var stranger = SSpawnAtPosition("MobHuman", testMap.GridCoords);

            void Send(EntityUid actor)
            {
                var msg = new StoriesHereticLearnMessage("STTestHereticKnowledgeRootA")
                {
                    Actor = actor,
                    UiKey = StoriesHereticKnowledgeUiKey.Key,
                    Entity = SEntMan.GetNetEntity(uid),
                };
                SEntMan.EventBus.RaiseLocalEvent(uid, (object) msg, true);
            }

            // Чужой клиент не может купить знание за еретика.
            Send(stranger);
            Assert.That(comp.Knowledge, Does.Not.Contain((EntProtoId) "STTestHereticKnowledgeRootA"));

            Send(uid);
            Assert.That(comp.Knowledge, Does.Contain((EntProtoId) "STTestHereticKnowledgeRootA"));

            // Чужой не может открыть окно знаний еретика (проверка ввода у него отключена).
            ui.OpenUi(uid, StoriesHereticKnowledgeUiKey.Key, stranger);
            Assert.That(ui.IsUiOpen(uid, StoriesHereticKnowledgeUiKey.Key, stranger), Is.False);

            // Повторное добавление компонента не дублирует действия.
            before = actions.GetActions(uid).Count();
            beforeEntities = ActionEntities(uid);
            Assert.That(beforeEntities, Is.GreaterThan(0));
            SEntMan.RemoveComponent<StoriesHereticComponent>(uid);
            SEntMan.AddComponent<StoriesHereticComponent>(uid);
            Assert.That(actions.GetActions(uid).Count(), Is.EqualTo(before), "Старые действия остались дублями");
        });

        // Удаление сущностей старых действий отложено до конца тика.
        await Pair.RunTicksSync(5);
        await Server.WaitAssertion(() =>
        {
            Assert.That(ActionEntities(holder), Is.EqualTo(beforeEntities), "Сущности старых действий остались в контейнере");
        });
    }

    [Test]
    public async Task FeastOfOwlsLocksAscension()
    {
        var testMap = await Pair.CreateTestMap();
        var sys = Server.System<StoriesHereticSystem>();

        await Server.WaitAssertion(() =>
        {
            // Пир сов: +5 очков, закрывает Вознесение и не идёт в цель «изучить N».
            var feasterUid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var feaster = SEntMan.AddComponent<StoriesHereticComponent>(feasterUid);
            var feasterEnt = new Entity<StoriesHereticComponent>(feasterUid, feaster);
            var points = feaster.Points;
            Assert.That(sys.TryLearn(feasterEnt, "STTestHereticKnowledgeFeast"), Is.True);
            Assert.That(feaster.CanAscend, Is.False);
            Assert.That(feaster.LearnedCount, Is.Zero);
            Assert.That(feaster.Points, Is.EqualTo(points + 5));

            // Знание Вознесения уже изучено — Пир сов закрыт.
            var ascenderUid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var ascender = SEntMan.AddComponent<StoriesHereticComponent>(ascenderUid);
            var ascenderEnt = new Entity<StoriesHereticComponent>(ascenderUid, ascender);
            Assert.That(sys.TryLearn(ascenderEnt, "STTestHereticKnowledgeRootA"), Is.True);
            ascender.LearnedCount = ascender.RequiredKnowledge;
            ascender.SacrificeCount = ascender.RequiredSacrifices;
            ascender.HeadSacrificeCount = ascender.RequiredHeadSacrifices;
            Assert.That(sys.TryLearn(ascenderEnt, "STTestHereticKnowledgeAscend"), Is.True);
            Assert.That(sys.CanLearn(ascenderEnt, "STTestHereticKnowledgeFeast", out _), Is.False);

            // Уже вознёсшийся — тоже.
            var ascendedUid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var ascended = SEntMan.AddComponent<StoriesHereticComponent>(ascendedUid);
            var ascendedEnt = new Entity<StoriesHereticComponent>(ascendedUid, ascended);
            sys.SetAscended(ascendedEnt);
            Assert.That(sys.CanLearn(ascendedEnt, "STTestHereticKnowledgeFeast", out _), Is.False);
        });
    }

    [Test]
    public async Task AuraAppearsLateAndHidesAfterFeast()
    {
        var testMap = await Pair.CreateTestMap();
        var sys = Server.System<StoriesHereticSystem>();
        var examine = Server.System<ExamineSystemShared>();

        await Server.WaitAssertion(() =>
        {
            // Как в tg: аура появляется, когда получено больше PointsToAura очков.
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            var examiner = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var auraText = FormattedMessage.RemoveMarkupOrThrow(Loc.GetString("stories-heretic-aura-examine", ("ent", uid)));

            sys.AddPoints(heretic, heretic.Comp.PointsToAura);
            Assert.That(heretic.Comp.AuraEntity, Is.Null, "Аура слишком рано");
            Assert.That(examine.GetExamineText(uid, examiner).ToString(), Does.Not.Contain(auraText));

            sys.AddPoints(heretic, 1);
            var aura = heretic.Comp.AuraEntity!.Value;
            Assert.That(SEntMan.GetComponent<TransformComponent>(aura).ParentUid, Is.EqualTo(uid));
            Assert.That(examine.GetExamineText(uid, examiner).ToString(), Does.Contain(auraText));

            sys.AddPoints(heretic, 1);
            Assert.That(heretic.Comp.AuraEntity, Is.EqualTo(aura), "Аура продублировалась");

            // Пир сов прячет ауру.
            Assert.That(sys.TryLearn(heretic, "STTestHereticKnowledgeFeast"), Is.True);
            Assert.That(heretic.Comp.AuraEntity, Is.Null);
            Assert.That(SEntMan.Deleted(aura) || SEntMan.IsQueuedForDeletion(aura), Is.True);
            Assert.That(examine.GetExamineText(uid, examiner).ToString(), Does.Not.Contain(auraText));

            sys.AddPoints(heretic, 10);
            Assert.That(heretic.Comp.AuraEntity, Is.Null, "Аура после Пира сов");

            // Снятие компонента еретика убирает ауру.
            var otherUid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var other = new Entity<StoriesHereticComponent>(otherUid, SEntMan.AddComponent<StoriesHereticComponent>(otherUid));
            sys.AddPoints(other, other.Comp.PointsToAura + 1);
            var otherAura = other.Comp.AuraEntity!.Value;
            SEntMan.RemoveComponent<StoriesHereticComponent>(otherUid);
            Assert.That(SEntMan.Deleted(otherAura) || SEntMan.IsQueuedForDeletion(otherAura), Is.True);
        });
    }
}
