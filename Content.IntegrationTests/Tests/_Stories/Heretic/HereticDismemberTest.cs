#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticDismemberTest : GameTest
{
    [Test]
    public async Task DismemberOnlyCorpses()
    {
        var testMap = await Pair.CreateTestMap();
        var dismember = Server.System<StoriesHereticDismemberSystem>();
        var damageable = Server.System<DamageableSystem>();
        var containers = Server.System<SharedContainerSystem>();

        await Server.WaitAssertion(() =>
        {
            var victim = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            EntityUid Head() => containers.GetContainer(victim, BodyComponent.ContainerID).ContainedEntities
                .First(o => SEntMan.GetComponent<OrganComponent>(o).Category == "Head");

            Assert.That(dismember.Dismember(victim, Head()), Is.Null, "Голову отрезали живому");

            damageable.TryChangeDamage(victim, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            var head = Head();
            Assert.That(dismember.Dismember(victim, head), Is.Not.Null);
            Assert.That(containers.GetContainer(victim, BodyComponent.ContainerID).ContainedEntities
                .Any(o => SEntMan.GetComponent<OrganComponent>(o).Category == "Head"), Is.False, "Голова осталась");
        });
    }
}
