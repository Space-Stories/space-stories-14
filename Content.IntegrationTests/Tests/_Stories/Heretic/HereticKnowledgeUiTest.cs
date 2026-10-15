#nullable enable
using System.Collections.Generic;
using Content.Client._Stories.Heretic.UI;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic.Components;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticKnowledgeUiTest : GameTest
{
    private static readonly ResPath KnowledgeRsi = new("/Textures/_Stories/Heretic/knowledge.rsi");

    private static readonly string[] Paths = ["Ash", "Flesh", "Void", "Rust", "Blade", "Cosmos", "Lock", "Moon"];

    [Test]
    public async Task RingColumnsFollowParadiseOrder()
    {
        await Pair.Client.WaitAssertion(() =>
        {
            var columns = StoriesHereticRing.BuildColumns(
                ["STHereticPathVoid", "STHereticPathAsh", "STHereticPathBlade", "STHereticPathFlesh"],
                [("STHereticPathFlesh", "STHereticPathVoid"), ("STHereticPathBlade", "STHereticPathVoid"), ("STHereticPathFlesh", "STHereticPathLock")]);

            Assert.That(columns, Is.EqualTo(new List<(string?, string?)>
            {
                (null, null),
                ("STHereticPathAsh", null),
                ("STHereticPathFlesh", "STHereticPathLock"), // полумост: соседа нет, мост прижат к нашему пути с его стороны
                ("STHereticPathFlesh", null),
                ("STHereticPathFlesh", "STHereticPathVoid"),
                ("STHereticPathVoid", null),
                ("STHereticPathVoid", "STHereticPathBlade"),
                ("STHereticPathBlade", null),
            }));

            Assert.That(StoriesHereticRing.ColumnOf(columns, null, ["STHereticPathVoid", "STHereticPathFlesh"]), Is.EqualTo(4));
            Assert.That(StoriesHereticRing.ColumnOf(columns, "STHereticPathBlade", []), Is.EqualTo(7));
            Assert.That(StoriesHereticRing.ColumnOf(columns, null, []), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Два знания в одной клетке окна (колонка, ступень, дорожка) рисуются друг поверх друга.
    /// </summary>
    [Test]
    public async Task KnowledgeCellsAreUnique()
    {
        var client = Pair.Client;
        var proto = client.ResolveDependency<IPrototypeManager>();
        var factory = client.ResolveDependency<IComponentFactory>();

        await client.WaitAssertion(() =>
        {
            var cells = new Dictionary<string, string>();
            foreach (var entity in proto.EnumeratePrototypes<EntityPrototype>())
            {
                // Тестовые прототипы (STTest*) в окно игрока не попадают.
                if (entity.Abstract || !entity.ID.StartsWith("STHereticKnowledge")
                    || !entity.TryGetComponent<StoriesHereticKnowledgeComponent>(out var knowledge, factory)
                    || knowledge.Starting)
                    continue;

                var bridge = string.Join("+", knowledge.Bridge);
                var cell = $"{knowledge.Path}|{bridge}|{knowledge.Stage}|{knowledge.Side}";
                Assert.That(cells.TryAdd(cell, entity.ID), Is.True, $"{entity.ID} и {cells.GetValueOrDefault(cell)} в одной клетке окна");
            }
        });
    }

    [Test]
    public async Task KnowledgeWindowBuilds()
    {
        var client = Pair.Client;
        var proto = client.ResolveDependency<IPrototypeManager>();
        var resources = client.ResolveDependency<IResourceCache>();
        var factory = client.ResolveDependency<IComponentFactory>();

        await client.WaitAssertion(() =>
        {
            bool HasState(ResPath rsi, string state)
            {
                return resources.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / rsi, out var res)
                       && res.RSI.TryGetState(state, out _);
            }

            // У каждого пути и у общих знаний есть фон узла.
            Assert.That(HasState(KnowledgeRsi, StoriesHereticPalette.General.NodeState));
            Assert.That(HasState(KnowledgeRsi, "node_locked"));
            foreach (var path in Paths)
            {
                var palette = StoriesHereticPalette.Get($"STHereticPath{path}");
                Assert.That(palette, Is.Not.EqualTo(StoriesHereticPalette.General), path);
                Assert.That(HasState(KnowledgeRsi, palette.NodeState), path);
            }

            // Иконки знаний указывают на существующие состояния RSI.
            foreach (var entity in proto.EnumeratePrototypes<EntityPrototype>())
            {
                if (!entity.TryGetComponent<StoriesHereticKnowledgeComponent>(out var knowledge, factory)
                    || knowledge.Icon is not SpriteSpecifier.Rsi icon)
                    continue;

                Assert.That(HasState(icon.RsiPath, icon.RsiState), entity.ID);
                // Многонаправленная иконка в окне режется до одного кадра из сетки.
                resources.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / icon.RsiPath).RSI.TryGetState(icon.RsiState, out var state);
                Assert.That(state!.RsiDirections, Is.EqualTo(RsiDirectionType.Dir1), $"{entity.ID}: иконка знания не однонаправленная");
            }

            // Окно собирается со всеми состояниями узлов, с иконкой и без.
            var grasp = new SpriteSpecifier.Rsi(KnowledgeRsi, "grasp_ash");
            var window = new StoriesHereticKnowledgeWindow();
            window.UpdateState(1,
                "STHereticPathAsh",
                "Ash",
                "items",
                [new(null, "General"), new("STHereticPathAsh", "Ash")],
                [
                    new("a", "A", "desc", 1, 1, 1, "STHereticPathAsh", grasp, [], StoriesHereticNodeState.Learned, null),
                    new("b", "B", "desc", 2, 1, 2, "STHereticPathAsh", null, ["a"], StoriesHereticNodeState.Available, null),
                    new("c", "C", "desc", 1, 0, 2, null, grasp, ["b"], StoriesHereticNodeState.Locked, "reason"),
                ]);
            window.Dispose();
        });
    }
}
