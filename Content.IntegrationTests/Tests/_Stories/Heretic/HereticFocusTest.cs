using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Inventory;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticFocusTest : GameTest
{
    [Test]
    public async Task AmberFocusGivesFocus()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var inventory = Server.System<InventorySystem>();

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            SEntMan.AddComponent<StoriesHereticComponent>(uid);
            Assert.That(heretics.HasFocus(uid), Is.False);

            var focus = SSpawnAtPosition("STHereticAmberFocus", testMap.GridCoords);
            Assert.That(inventory.TryEquip(uid, focus, "neck", silent: true, force: true), Is.True);
            Assert.That(heretics.HasFocus(uid), Is.True);
        });
    }
}
