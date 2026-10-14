#nullable enable
using Content.Client._Stories.Heretic;
using Content.IntegrationTests.Fixtures;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticMarkVisualsTest : GameTest
{
    [Test]
    public async Task MarksRsiHasAllStates()
    {
        var resources = Pair.Client.ResolveDependency<IResourceCache>();

        await Pair.Client.WaitAssertion(() =>
        {
            Assert.That(resources.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / StoriesHereticMarkVisualsSystem.Rsi, out var res), "marks.rsi не найден");
            for (var i = 1; i <= 8; i++)
            {
                Assert.That(res!.RSI.TryGetState($"emark{i}", out _), $"нет state emark{i}");
            }

            Assert.That(StoriesHereticMarkVisualsSystem.States, Has.Count.EqualTo(8));
            foreach (var state in StoriesHereticMarkVisualsSystem.States.Values)
            {
                Assert.That(res!.RSI.TryGetState(state, out _), state);
            }
        });
    }
}
