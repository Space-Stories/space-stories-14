#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Organs;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticOrganTest : GameTest
{
    [Test]
    public async Task SacrificeCursesTwoToFourOrgans()
    {
        var testMap = await Pair.CreateTestMap();
        var organs = Server.System<StoriesHereticCorruptedOrganSystem>();
        var containers = Server.System<SharedContainerSystem>();

        await Server.WaitAssertion(() =>
        {
            var victim = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var replaced = organs.CurseOrgans(victim, testMap.GridCoords);
            Assert.That(replaced, Is.InRange(2, 4));

            var inBody = containers.GetContainer(victim, BodyComponent.ContainerID).ContainedEntities
                .Count(SEntMan.HasComponent<StoriesHereticCorruptedOrganComponent>);
            Assert.That(inBody, Is.EqualTo(replaced), "Испорченные органы не в теле");

            // Старые органы выпали на пол.
            var dropped = 0;
            var query = SEntMan.AllEntityQueryEnumerator<OrganComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var organ, out var xform))
            {
                if (organ.Body == null && !containers.IsEntityInContainer(uid) && !SEntMan.HasComponent<StoriesHereticCorruptedOrganComponent>(uid))
                    dropped++;
            }

            Assert.That(dropped, Is.EqualTo(replaced), "Старые органы не выпали");
            Assert.That(SEntMan.HasComponent<StoriesHereticCorruptedBodyComponent>(victim));
        });
    }
}
