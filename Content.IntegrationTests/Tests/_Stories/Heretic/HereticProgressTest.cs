#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticProgressTest : GameTest
{
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

            // Тело утрачено (гиб) — прогресс и Вознесение берутся со снимка роли.
            SDeleteNow(body);
            Assert.That(mind.OwnedEntity, Is.Not.EqualTo(body));
            Assert.That(objectives.IsCompleted(sacrificeObjective, (mindId, mind)));
            Assert.That(objectives.IsCompleted(headObjective, (mindId, mind)));
            Assert.That(progress.TryGetProgress(mindId, mind, out var snapshot));
            Assert.That(snapshot!.Ascended);
        });
    }
}
