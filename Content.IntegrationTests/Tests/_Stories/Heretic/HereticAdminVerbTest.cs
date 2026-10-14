#pragma warning disable RA0002
using Content.IntegrationTests.Tests.GameRules;
using Content.Server._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Antag;
using Content.Server.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticAdminVerbTest : AntagTest
{
    private static readonly ProtoId<AntagSpecifierPrototype> Specifier = "STHereticSpecifier";

    /// <summary>
    /// Админ-верб «Сделать еретиком» вызывает ForceMakeAntag с правилом STHeretic — игрок в раунде должен стать еретиком.
    /// </summary>
    [Test]
    public async Task ForceMakeHereticInRound()
    {
        await Server.WaitPost(() => STicker.ToggleReadyAll(true));
        await Pair.RunUntilSynced();
        await Pair.WaitCommand("setgamepreset Extended");
        await Pair.WaitCommand("startround");
        await Pair.RunUntilSynced();

        await Server.WaitPost(() =>
        {
            Assert.That(STicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            AntagSys.ForceMakeAntag<StoriesHereticRuleComponent>(Pair.Player!, "STHeretic", Specifier);
        });
        await Pair.RunTicksSync(5);

        await Server.WaitAssertion(() =>
        {
            var player = Pair.Player!.AttachedEntity!.Value;
            Assert.That(SEntMan.HasComponent<StoriesHereticComponent>(player), "Верб не сделал игрока еретиком");
            SAssertAntagInitialized(SProtoMan.Index(Specifier), Pair.Player!);
        });
    }
}
