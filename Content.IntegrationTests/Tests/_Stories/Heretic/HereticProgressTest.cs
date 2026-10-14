#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticProgressTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: STTestHereticKnowledgeSummoner
  components:
  - type: StoriesHereticKnowledge
    requiredSummons: 2
    objectives: [ STHereticObjectiveSummon ]
";

    [Test]
    public async Task KnowledgeAddsSummonObjective()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var minds = Server.System<SharedMindSystem>();

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var mindId = minds.CreateMind(null);
            minds.TransferTo(mindId, uid);
            Server.System<RoleSystem>().MindAddRole(mindId, "STMindRoleHeretic");
            var ent = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));

            heretics.AddPoints(ent, 5);
            Assert.That(heretics.TryLearn(ent, "STTestHereticKnowledgeSummoner"), Is.True);
            Assert.That(ent.Comp.RequiredSummons, Is.EqualTo(2));

            var mind = SEntMan.GetComponent<MindComponent>(mindId);
            Assert.That(mind.Objectives.Any(o => SEntMan.GetComponent<MetaDataComponent>(o).EntityPrototype?.ID == "STHereticObjectiveSummon"));

            ent.Comp.SacrificeCount = ent.Comp.RequiredSacrifices;
            ent.Comp.HeadSacrificeCount = ent.Comp.RequiredHeadSacrifices;
            ent.Comp.LearnedCount = ent.Comp.RequiredKnowledge;
            Assert.That(heretics.ObjectivesComplete(ent.Comp), Is.False, "Цель призывов не учтена");

            var player = new NetUserId(Guid.NewGuid());
            Assert.That(heretics.RegisterSummon(ent, player), Is.True);
            Assert.That(heretics.RegisterSummon(ent, player), Is.False, "Один игрок засчитан дважды");
            Assert.That(heretics.RegisterSummon(ent, new NetUserId(Guid.NewGuid())), Is.True);
            Assert.That(heretics.ObjectivesComplete(ent.Comp), Is.True);
        });
    }

    [Test]
    public async Task ProgressSurvivesBodyLoss()
    {
        var testMap = await Pair.CreateTestMap();
        var minds = Server.System<MindSystem>();
        var roles = Server.System<RoleSystem>();
        var objectives = Server.System<SharedObjectivesSystem>();
        var progress = Server.System<StoriesHereticObjectiveSystem>();
        var sacrifice = Server.System<StoriesHereticSacrificeSystem>();

        await Server.WaitAssertion(() =>
        {
            var body = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var heretic = SEntMan.AddComponent<StoriesHereticComponent>(body);
            var mindId = minds.CreateMind(null);
            var mind = SEntMan.GetComponent<MindComponent>(mindId);
            minds.TransferTo(mindId, body, mind: mind);
            roles.MindAddRole(mindId, "STMindRoleHeretic", mind, silent: true);
            Assert.That(minds.TryAddObjective(mindId, mind, "STHereticObjectiveSacrifice"));
            Assert.That(minds.TryAddObjective(mindId, mind, "STHereticObjectiveHeadSacrifice"));
            var sacrificeObjective = mind.Objectives[0];
            var headObjective = mind.Objectives[1];

            heretic.SacrificeCount = heretic.RequiredSacrifices;
            heretic.Ascended = true;
            Assert.That(objectives.IsCompleted(sacrificeObjective, (mindId, mind)));
            Assert.That(objectives.IsCompleted(headObjective, (mindId, mind)), Is.False);

            // Глав нет — требование снимается, заголовок цели обновляется.
            sacrifice.AssignTargets((body, heretic));
            Assert.That(objectives.IsCompleted(headObjective, (mindId, mind)));
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(headObjective).EntityName,
                Is.EqualTo(Loc.GetString("stories-heretic-objective-head-sacrifice", ("count", 0))));

            // Счётчик меняется уже после последнего чтения прогресса — снимок обязан сохранить OnMindRemoved.
            heretic.LearnedCount = 5;

            // Тело утрачено (гиб) — прогресс и Вознесение берутся со снимка роли.
            SDeleteNow(body);
            Assert.That(mind.OwnedEntity, Is.Not.EqualTo(body));
            Assert.That(objectives.IsCompleted(sacrificeObjective, (mindId, mind)));
            Assert.That(objectives.IsCompleted(headObjective, (mindId, mind)));
            Assert.That(progress.TryGetProgress(mindId, mind, out var snapshot));
            Assert.That(snapshot!.Ascended);
            Assert.That(snapshot.LearnedCount, Is.EqualTo(5), "Снимок не сохранён при уходе разума");
        });
    }
}
