#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Stories.Heretic.Organs;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

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

    private const string Blindness = "StatusEffectBlindness";

    private async Task<EntityUid> SetupEyesScene(bool holyWater)
    {
        var testMap = await Pair.CreateTestMap();
        var organs = Server.System<StoriesHereticCorruptedOrganSystem>();
        var inventory = Server.System<InventorySystem>();
        var solutions = Server.System<SharedSolutionContainerSystem>();
        EntityUid victim = default;

        await Server.WaitAssertion(() =>
        {
            victim = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            Assert.That(organs.TryCurseOrgan(victim, "Eyes", testMap.GridCoords), Is.True);

            var heretic = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            SEntMan.AddComponent<StoriesHereticComponent>(heretic);
            var focus = SSpawnAtPosition("STHereticAmberFocus", testMap.GridCoords);
            Assert.That(inventory.TryEquip(heretic, focus, "neck", silent: true, force: true), Is.True);

            if (holyWater)
            {
                Assert.That(solutions.TryGetSolution(victim, BloodstreamComponent.DefaultBloodSolutionName, out var blood));
                Assert.That(solutions.TryAddReagent(blood!.Value, "Holywater", 5));
            }
        });

        return victim;
    }

    [Test]
    public async Task CorruptedEyesBlindNearbyFocusHolder()
    {
        var victim = await SetupEyesScene(holyWater: false);
        await RunSeconds(2);
        Assert.That(Server.System<StatusEffectsSystem>().HasStatusEffect(victim, Blindness), Is.True);
    }

    [Test]
    public async Task HolyWaterSuppressesCorruptedEyes()
    {
        var victim = await SetupEyesScene(holyWater: true);
        await RunSeconds(2);
        Assert.That(Server.System<StatusEffectsSystem>().HasStatusEffect(victim, Blindness), Is.False);
    }

    [Test]
    public async Task RemovingCorruptedOrganFromLivingBodyBursts()
    {
        var testMap = await Pair.CreateTestMap();
        var organs = Server.System<StoriesHereticCorruptedOrganSystem>();
        var containers = Server.System<SharedContainerSystem>();
        EntityUid organ = default;
        EntityUid victim = default;

        await Server.WaitAssertion(() =>
        {
            victim = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            Assert.That(organs.TryCurseOrgan(victim, "Liver", testMap.GridCoords), Is.True);
            var container = containers.GetContainer(victim, BodyComponent.ContainerID);
            organ = container.ContainedEntities.Single(SEntMan.HasComponent<StoriesHereticCorruptedOrganComponent>);
            SEntMan.GetComponent<StoriesHereticCorruptedOrganComponent>(organ).BurstChance = 1f;
            Assert.That(SEntMan.HasComponent<StoriesHereticCorruptedBodyComponent>(victim));

            containers.Remove(organ, container);
        });

        await RunTicksSync(2);
        Assert.That(SEntMan.Deleted(organ), "Орган не взорвался");
        Assert.That(SEntMan.HasComponent<StoriesHereticCorruptedBodyComponent>(victim), Is.False, "Маркер тела не снят");
    }

    [Test]
    public async Task CorruptedHeartDrainsStamina()
    {
        var testMap = await Pair.CreateTestMap();
        var organs = Server.System<StoriesHereticCorruptedOrganSystem>();
        var containers = Server.System<SharedContainerSystem>();
        var timing = Server.ResolveDependency<IGameTiming>();
        EntityUid victim = default;

        await Server.WaitAssertion(() =>
        {
            victim = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            Assert.That(organs.TryCurseOrgan(victim, "Heart", testMap.GridCoords), Is.True);
            var heart = containers.GetContainer(victim, BodyComponent.ContainerID).ContainedEntities
                .Single(SEntMan.HasComponent<StoriesHereticCorruptedOrganComponent>);
            var comp = SEntMan.GetComponent<StoriesHereticCorruptedOrganComponent>(heart);
            Assert.That(comp.NextEffect, Is.GreaterThan(timing.CurTime), "Расписание сердца не начато при проклятии");
            comp.NextEffect = timing.CurTime;
        });

        await RunSeconds(2);
        Assert.That(SEntMan.GetComponent<StaminaComponent>(victim).StaminaDamage, Is.GreaterThan(0));
    }
}
