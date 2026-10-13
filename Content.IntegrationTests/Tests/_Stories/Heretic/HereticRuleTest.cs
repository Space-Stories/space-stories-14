using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticRuleTest : GameTest
{
    private const string RuleId = "STHeretic";
    private const string AntagId = "STHeretic";

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    [Test]
    public async Task RuleCreatesHeretic()
    {
        var pair = Pair;
        var server = pair.Server;
        var ticker = server.System<GameTicker>();
        var mindSys = server.System<MindSystem>();
        var roleSys = server.System<RoleSystem>();

        var minPlayers = 1;
        await server.WaitAssertion(() =>
        {
            var proto = SProtoMan.Index<EntityPrototype>(RuleId);
            Assert.That(proto.TryComp<GameRuleComponent>(out var rule, server.ResolveDependency<IComponentFactory>()));
            minPlayers = rule!.MinPlayers;
        });

        await pair.Server.AddDummySessions(minPlayers);
        await pair.RunTicksSync(5);
        await pair.SetAntagPreference(AntagId, true);

        await server.WaitPost(() =>
        {
            var ruleEnt = ticker.AddGameRule(RuleId);
            ticker.ToggleReadyAll(true);
            ticker.StartRound();
            ticker.StartGameRule(ruleEnt);
        });
        await pair.RunTicksSync(10);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        var player = pair.Player!.AttachedEntity!.Value;

        await server.WaitAssertion(() =>
        {
            var mind = mindSys.GetMind(player)!.Value;
            Assert.That(roleSys.MindIsAntagonist(mind));
            Assert.That(SEntMan.TryGetComponent<StoriesHereticComponent>(player, out var heretic));
            Assert.That(heretic!.KnowledgeRitualTags, Has.Count.EqualTo(4), "MapInit еретика не отработал");
            Assert.That(SEntMan.GetComponent<MindComponent>(mind).Objectives, Has.Count.EqualTo(3));
        });
    }
}
