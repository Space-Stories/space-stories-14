using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server._Stories.PocketDimension;
using Content.Shared._Stories.PocketDimension;
using Content.Shared.Damage.Components;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Stories;

[TestOf(typeof(PocketDimensionSystem))]
public sealed class PocketDimensionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [SidedDependency(Side.Server)] private PocketDimensionSystem _pocket = default!;
    [SidedDependency(Side.Server)] private SharedMapSystem _map = default!;
    [SidedDependency(Side.Server)] private ITileDefinitionManager _tileDef = default!;

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

    [Test]
    [Description("Комнату нельзя разобрать или покинуть в обход двери: пол неразрушим, порталы гаснут, " +
                 "вылетевшее за стены возвращается, а удалённая внутренняя дверь пересоздаётся.")]
    public async Task RoomProtectionTest()
    {
        var wall = ToServer(await SpawnTarget("WallSolid"));
        var sticker = ToServer(await PlaceInHands("STPocketDimensionSticker"));
        var comp = SEntMan.GetComponent<PocketDimensionStickerComponent>(sticker);

        await Server.WaitPost(() => _pocket.Stick((sticker, comp), wall, SPlayer));
        await RunTicks(1);
        var outer = comp.OuterDoor!.Value;
        var pocketMap = comp.Map!.Value;
        var grid = SEntMan.GetComponent<MapGridComponent>(pocketMap);

        await Interact(outer, SEntMan.GetComponent<TransformComponent>(outer).Coordinates);
        Assert.That(SEntMan.GetComponent<TransformComponent>(SPlayer).MapUid, Is.EqualTo(pocketMap));

        EntityUid portal = default;
        EntityUid apple = default;
        await Server.WaitPost(() =>
        {
            // Пол не снимается ни ломом, ни РСУ, ни взрывом.
            var center = _map.GetTileRef(pocketMap, grid, new Vector2i(2, 2));
            var floor = (ContentTileDefinition) _tileDef[center.Tile.TypeId];
            Assert.That(floor.Indestructible, Is.True);
            Assert.That(floor.DeconstructTools, Is.Empty);

            portal = SEntMan.SpawnEntity("PortalRed", SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
            apple = SEntMan.SpawnEntity("FoodApple", _map.GridTileToLocal(pocketMap, grid, new Vector2i(10, 10)));
        });
        await RunTicks(60);

        Assert.That(SEntMan.Deleted(portal), Is.True, "Портал в Изнанке должен сразу гаснуть.");
        await Server.WaitAssertion(() =>
        {
            var tile = _map.LocalToTile(pocketMap, grid, SEntMan.GetComponent<TransformComponent>(apple).Coordinates);
            Assert.That(tile.X, Is.InRange(1, 3), "Предмет за стенами должен вернуться в комнату.");
            Assert.That(tile.Y, Is.InRange(1, 3), "Предмет за стенами должен вернуться в комнату.");
        });

        // Выходим, удаляем внутреннюю дверь и заходим снова: без ошибок, дверь появляется заново.
        var inner = comp.InnerDoor!.Value;
        await Interact(inner, SEntMan.GetComponent<TransformComponent>(inner).Coordinates);
        Assert.That(SEntMan.GetComponent<TransformComponent>(SPlayer).MapUid, Is.Not.EqualTo(pocketMap), "Портал не должен был сдвинуть игрока от двери.");
        await Server.WaitPost(() => SEntMan.DeleteEntity(inner));

        await Interact(outer, SEntMan.GetComponent<TransformComponent>(outer).Coordinates);
        Assert.That(SEntMan.GetComponent<TransformComponent>(SPlayer).MapUid, Is.EqualTo(pocketMap));
        Assert.That(SEntMan.EntityExists(comp.InnerDoor), Is.True, "Удалённая внутренняя дверь должна пересоздаться.");
    }
}
