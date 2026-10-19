#nullable enable
using Content.Client._Stories.Heretic.UI;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic.Components;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
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

            Assert.That(HasState(KnowledgeRsi, StoriesHereticPalette.General.NodeState));
            Assert.That(HasState(KnowledgeRsi, "node_locked"));
            foreach (var path in Paths)
            {
                var palette = StoriesHereticPalette.Get($"STHereticPath{path}");
                Assert.That(palette, Is.Not.EqualTo(StoriesHereticPalette.General), path);
                Assert.That(HasState(KnowledgeRsi, palette.NodeState), path);
            }

            foreach (var entity in proto.EnumeratePrototypes<EntityPrototype>())
            {
                if (!entity.TryGetComponent<StoriesHereticKnowledgeComponent>(out var knowledge, factory)
                    || knowledge.Icon is not SpriteSpecifier.Rsi icon)
                    continue;

                Assert.That(HasState(icon.RsiPath, icon.RsiState), entity.ID);
            }

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
