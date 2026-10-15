#nullable enable
using Content.Client._Stories.Heretic;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic.Components;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
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

    [Test]
    public async Task MarkLayerFollowsPath()
    {
        var testMap = await Pair.CreateTestMap();
        var timing = Server.ResolveDependency<IGameTiming>();
        var target = EntityUid.Invalid;
        StoriesHereticMarkComponent mark = default!;

        bool HasLayer() => Client.System<SpriteSystem>().LayerMapTryGet(
            (CEntMan.GetEntity(SEntMan.GetNetEntity(target)), null), StoriesHereticMarkVisualsSystem.LayerKey, out _, false);

        await Server.WaitPost(() =>
        {
            target = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            mark = new StoriesHereticMarkComponent { Path = "STHereticPathFlesh", EndTime = timing.CurTime + TimeSpan.FromMinutes(1) };
            SEntMan.AddComponent(target, mark);
        });
        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() => Assert.That(HasLayer(), Is.True, "Нет печати метки"));

        // Путь, которого нет в таблице печатей: старая печать снимается.
        await Server.WaitPost(() =>
        {
            mark.Path = "Meat";
            SEntMan.Dirty(target, mark);
        });
        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() => Assert.That(HasLayer(), Is.False, "Печать осталась у метки с чужим путём"));
    }

    [Test]
    public async Task WaitingSummonRestoresSpriteColor()
    {
        var testMap = await Pair.CreateTestMap();
        var mob = EntityUid.Invalid;
        var tint = Color.Red;

        SpriteComponent Sprite() => CEntMan.GetComponent<SpriteComponent>(CEntMan.GetEntity(SEntMan.GetNetEntity(mob)));

        await Server.WaitPost(() => mob = SSpawnAtPosition("MobHuman", testMap.GridCoords));
        await Pair.RunTicksSync(5);
        await Client.WaitPost(() => Client.System<SpriteSystem>().SetColor((CEntMan.GetEntity(SEntMan.GetNetEntity(mob)), Sprite()), tint));

        await Server.WaitPost(() => SEntMan.AddComponent<StoriesHereticSummonWaitingComponent>(mob));
        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() => Assert.That(Sprite().Color, Is.EqualTo(tint.WithAlpha(0.5f)), "Ждущий призыв не полупрозрачен"));

        await Server.WaitPost(() => SEntMan.RemoveComponent<StoriesHereticSummonWaitingComponent>(mob));
        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() => Assert.That(Sprite().Color, Is.EqualTo(tint), "Цвет спрайта не вернулся"));
    }
}
