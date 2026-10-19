#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Damage;
using Content.Server.Mind;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Shared.Shuttles.Components;
using Content.Shared.Station.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.UnitTesting.Pool;

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

        var stranger = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var heretic = new Entity<StoriesHereticComponent>(uid, comp);
            var points = comp.Points;

            var rune = SSpawnAtPosition("STHereticRune", coords);
            stranger = SSpawnAtPosition("MobHuman", coords);
            damageable.TryChangeDamage(stranger, new DamageSpecifier { DamageDict = new() { { "Blunt", 300 } } }, true);
            Assert.That(mobState.IsDead(stranger));

            heretics.SetTargets(heretic, new());
            Assert.That(rituals.TryPerform(heretic, rune), Is.False);

            heretics.SetTargets(heretic, new() { stranger });
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(comp.SacrificeCount, Is.EqualTo(1));
            Assert.That(comp.Points, Is.EqualTo(points + comp.SacrificePoints));
            Assert.That(SEntMan.HasComponent<StoriesHereticSacrificedComponent>(stranger));
            Assert.That(mobState.IsAlive(stranger), "Жертва не исцелена");
            Assert.That(SEntMan.GetComponent<TransformComponent>(stranger).MapID, Is.Not.EqualTo(testMap.MapId),
                "Жертва не перенесена в Теневой мир");
        });

        await Server.WaitPost(() => SEntMan.GetComponent<StoriesHereticShadowRealmComponent>(stranger).ReturnTime = TimeSpan.Zero);
        await Pair.RunTicksSync(5);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<StoriesHereticShadowRealmComponent>(stranger), Is.False);
            Assert.That(SEntMan.GetComponent<TransformComponent>(stranger).MapID, Is.EqualTo(testMap.MapId),
                "Жертва не вернулась из Теневого мира");
        });
    }

    [Test]
    public async Task RerollSkipsSacrificed()
    {
        var testMap = await Pair.CreateTestMap();
        var sacrifice = Server.System<StoriesHereticSacrificeSystem>();
        var minds = Server.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            MakeStation(testMap);
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));

            var victim = SSpawnAtPosition("MobHuman", coords);
            var former = SSpawnAtPosition("MobHuman", coords);
            minds.TransferTo(minds.CreateMind(null), victim);
            minds.TransferTo(minds.CreateMind(null), former);
            SEntMan.AddComponent<StoriesHereticSacrificedComponent>(former);

            sacrifice.AssignTargets(heretic);
            Assert.That(heretic.Comp.Targets, Does.Contain(SEntMan.GetNetEntity(victim)));
            Assert.That(heretic.Comp.Targets, Does.Not.Contain(SEntMan.GetNetEntity(former)));
        });
    }

    private void MakeStation(TestMapData map)
    {
        SEntMan.EnsureComponent<StationMemberComponent>(map.Grid).Station = map.MapUid;
        SEntMan.EnsureComponent<StationDataComponent>(map.MapUid).Grids.Add(map.Grid.Owner);
    }

    [Test]
    public async Task HeadIsCommandStaffBody()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var sacrifice = Server.System<StoriesHereticSacrificeSystem>();
        var damageable = Server.System<DamageableSystem>();
        var minds = Server.System<MindSystem>();
        var roles = Server.System<RoleSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;

            var blueshield = SSpawnAtPosition("MobHuman", coords);
            var mind = minds.CreateMind(null);
            minds.TransferTo(mind, blueshield);
            roles.MindAddJobRole(mind, silent: true, jobPrototype: "STBlueshield");
            Assert.That(sacrifice.IsHead(blueshield), Is.False);

            var head = SSpawnAtPosition("MobHuman", coords);
            SEntMan.AddComponent<CommandStaffComponent>(head);
            Assert.That(sacrifice.IsHead(head));

            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            var rune = SSpawnAtPosition("STHereticRune", coords);
            damageable.TryChangeDamage(head, new DamageSpecifier { DamageDict = new() { { "Blunt", 300 } } }, true);
            heretics.SetTargets(heretic, new() { head });

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(heretic.Comp.HeadSacrificeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HeadWithoutMindIsSlotNotWaived()
    {
        var testMap = await Pair.CreateTestMap();
        var sacrifice = Server.System<StoriesHereticSacrificeSystem>();
        var damageable = Server.System<DamageableSystem>();

        await Server.WaitAssertion(() =>
        {
            MakeStation(testMap);
            var coords = testMap.GridCoords;

            var head = SSpawnAtPosition("MobHuman", coords);
            SEntMan.AddComponent<CommandStaffComponent>(head);
            damageable.TryChangeDamage(head, new DamageSpecifier { DamageDict = new() { { "Blunt", 300 } } }, true);

            var uid = SSpawnAtPosition("MobHuman", coords);
            var heretic = new Entity<StoriesHereticComponent>(uid, SEntMan.AddComponent<StoriesHereticComponent>(uid));
            sacrifice.AssignTargets(heretic);

            Assert.That(heretic.Comp.RequiredHeadSacrifices, Is.EqualTo(1));
            Assert.That(heretic.Comp.Targets, Does.Contain(SEntMan.GetNetEntity(head)));
        });
    }

    [Test]
    public async Task TargetsOnStationAndHeadWaiver()
    {
        var testMap = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        var sacrifice = Server.System<StoriesHereticSacrificeSystem>();
        var minds = Server.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            MakeStation(testMap);
            var coords = testMap.GridCoords;

            var first = SSpawnAtPosition("MobHuman", coords);
            var firstHeretic = new Entity<StoriesHereticComponent>(first, SEntMan.AddComponent<StoriesHereticComponent>(first));
            var crew = SSpawnAtPosition("MobHuman", coords);
            minds.TransferTo(minds.CreateMind(null), crew);
            sacrifice.AssignTargets(firstHeretic);
            Assert.That(firstHeretic.Comp.RequiredHeadSacrifices, Is.Zero);

            var head = SSpawnAtPosition("MobHuman", coords);
            SEntMan.AddComponent<CommandStaffComponent>(head);
            minds.TransferTo(minds.CreateMind(null), head);
            var offStation = SSpawnAtPosition("MobHuman", otherMap.GridCoords);
            minds.TransferTo(minds.CreateMind(null), offStation);

            SEntMan.EnsureComponent<StationMemberComponent>(otherMap.Grid).Station = testMap.MapUid;
            SEntMan.EnsureComponent<FTLComponent>(otherMap.Grid);

            var second = SSpawnAtPosition("MobHuman", coords);
            var secondHeretic = new Entity<StoriesHereticComponent>(second, SEntMan.AddComponent<StoriesHereticComponent>(second));
            sacrifice.AssignTargets(secondHeretic);
            Assert.That(secondHeretic.Comp.RequiredHeadSacrifices, Is.EqualTo(1));
            Assert.That(secondHeretic.Comp.Targets, Does.Contain(SEntMan.GetNetEntity(head)));
            Assert.That(secondHeretic.Comp.Targets, Does.Contain(SEntMan.GetNetEntity(crew)));
            Assert.That(secondHeretic.Comp.Targets, Does.Not.Contain(SEntMan.GetNetEntity(offStation)));

            SEntMan.RemoveComponent<FTLComponent>(otherMap.Grid);
        });
    }
}
