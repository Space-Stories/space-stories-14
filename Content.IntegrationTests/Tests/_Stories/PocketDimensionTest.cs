using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server._Stories.PocketDimension;
using Content.Shared._Stories.PocketDimension;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories;

[TestOf(typeof(PocketDimensionSystem))]
public sealed class PocketDimensionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [SidedDependency(Side.Server)] private PocketDimensionSystem _pocket = default!;

    [Test]
    [Description("Наклейка делает дверь в комнату, через двери можно ходить туда и обратно, а при уничтожении наклейки всех выбрасывает на станцию.")]
    public async Task EnterExitCollapseTest()
    {
        var wall = ToServer(await SpawnTarget("WallSolid"));
        var sticker = ToServer(await PlaceInHands("STPocketDimensionSticker"));
        var comp = SEntMan.GetComponent<PocketDimensionStickerComponent>(sticker);
        var stationMap = SEntMan.GetComponent<TransformComponent>(SPlayer).MapUid;

        // Настоящий клик по стене: вне станции наклейка не клеится.
        await Interact();
        Assert.That(comp.OuterDoor, Is.Null);

        await Server.WaitPost(() => _pocket.Stick((sticker, comp), wall, SPlayer));
        await RunTicks(1);

        Assert.That(comp.OuterDoor, Is.Not.Null);
        Assert.That(comp.Map, Is.Not.Null);
        var outer = comp.OuterDoor!.Value;
        var inner = comp.InnerDoor!.Value;

        // Входим.
        await Interact(outer, SEntMan.GetComponent<TransformComponent>(outer).Coordinates);
        Assert.That(SEntMan.GetComponent<TransformComponent>(SPlayer).MapUid, Is.EqualTo(comp.Map));

        // Выходим.
        await Interact(inner, SEntMan.GetComponent<TransformComponent>(inner).Coordinates);
        Assert.That(SEntMan.GetComponent<TransformComponent>(SPlayer).MapUid, Is.EqualTo(stationMap));

        // Входим снова и уничтожаем наклейку, пока сидим внутри.
        await Interact(outer, SEntMan.GetComponent<TransformComponent>(outer).Coordinates);
        Assert.That(SEntMan.GetComponent<TransformComponent>(SPlayer).MapUid, Is.EqualTo(comp.Map));
        var pocketMap = comp.Map!.Value;

        // Удар по стене засчитывается, а второй сразу следом — нет, из-за паузы между ударами.
        var innerCoords = SEntMan.GetComponent<TransformComponent>(inner).Coordinates;
        EntityUid roomWall = default;
        await Server.WaitPost(() =>
        {
            var query = SEntMan.EntityQueryEnumerator<PocketDimensionWallComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out _, out var xform))
            {
                if (xform.MapUid == pocketMap && xform.Coordinates.Position == innerCoords.Position)
                    roomWall = uid;
            }
        });
        Assert.That(roomWall, Is.Not.EqualTo(default(EntityUid)));
        Assert.That(SEntMan.HasComponent<DamageableComponent>(roomWall), "Без Damageable удары оружием по стене засчитываются как промах.");
        await Interact(roomWall, innerCoords);
        await Interact(roomWall, innerCoords);
        Assert.That(SEntMan.GetComponent<PocketDimensionMapComponent>(pocketMap).Hits, Is.EqualTo(1));

        await Server.WaitPost(() => SEntMan.DeleteEntity(sticker));
        await RunTicks(5);

        Assert.That(SEntMan.GetComponent<TransformComponent>(SPlayer).MapUid, Is.EqualTo(stationMap));
        Assert.That(SEntMan.Deleted(pocketMap), Is.True);
        Assert.That(SEntMan.Deleted(outer), Is.True);
    }
}
