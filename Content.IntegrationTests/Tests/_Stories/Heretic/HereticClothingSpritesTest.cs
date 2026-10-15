using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

/// <summary>
/// Клиент ищет вид на персонаже по имени слота из своей таблицы (голова — <c>equipped-HELMET</c>, не HEAD).
/// Без нужного состояния слой молча не рисуется: капюшон надет, а его не видно.
/// </summary>
[TestFixture]
public sealed class HereticClothingSpritesTest : GameTest
{
    private static readonly Dictionary<SlotFlags, string> States = new()
    {
        { SlotFlags.HEAD, "equipped-HELMET" },
        { SlotFlags.OUTERCLOTHING, "equipped-OUTERCLOTHING" },
        { SlotFlags.NECK, "equipped-NECK" },
        { SlotFlags.MASK, "equipped-MASK" },
    };

    [Test]
    public async Task HereticClothingHasEquippedStates()
    {
        var client = Pair.Client;
        await client.WaitIdleAsync();
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var factory = client.ResolveDependency<IComponentFactory>();

        await client.WaitAssertion(() =>
        {
            var checkedAny = false;
            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract || !proto.ID.StartsWith("STHeretic")
                    || !proto.TryGetComponent<ClothingComponent>(out var clothing, factory) || clothing.RsiPath == null)
                    continue;

                foreach (var (slot, state) in States)
                {
                    if ((clothing.Slots & slot) == 0)
                        continue;

                    checkedAny = true;
                    var path = new Robust.Shared.Utility.ResPath($"/Textures/{clothing.RsiPath}/{state}.png");
                    Assert.That(resMan.ContentFileExists(path), Is.True, $"{proto.ID}: нет {path}");
                }
            }

            Assert.That(checkedAny, Is.True, "Не нашлось одежды Еретика");
        });
    }
}
