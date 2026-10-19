using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Stories.DisguiseRemote;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mindshield;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories;

[TestOf(typeof(DisguisedComponent))]
public sealed class DisguiseRemoteTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    private const string TargetName = "Артур Пирожков";
    private const string TargetSuit = "ClothingUniformJumpsuitBartender";

    [SidedDependency(Side.Server)] private InventorySystem _inventory = default!;
    [SidedDependency(Side.Server)] private SharedIdCardSystem _idCard = default!;
    [SidedDependency(Side.Server)] private SharedAccessSystem _access = default!;
    [SidedDependency(Side.Server)] private MetaDataSystem _metaData = default!;
    [SidedDependency(Side.Server)] private SharedSubdermalImplantSystem _implants = default!;
    [SidedDependency(Side.Server)] private MindShieldSystem _mindShield = default!;
    [SidedDependency(Side.Client)] private SpriteSystem _sprite = default!;

    [Test]
    [Description("Пульт копирует имя, вещи и ID-карту с доступами цели, исчезает, а способность меняет облики и комплекты вещей местами.")]
    public async Task CopyAndToggleTest()
    {
        var sTarget = ToServer(await SpawnTarget("MobHuman"));
        EntityUid ownSuit = default;
        var ownName = SEntMan.GetComponent<MetaDataComponent>(SPlayer).EntityName;

        await Server.WaitAssertion(() =>
        {
            _metaData.SetEntityName(sTarget, TargetName);
            Assert.That(_inventory.SpawnItemInSlot(sTarget, "jumpsuit", TargetSuit));
            Assert.That(_inventory.SpawnItemInSlot(sTarget, "id", "PassengerPDA"));
            Assert.That(_idCard.TryFindIdCard(sTarget, out var id));
            _idCard.TryChangeFullName(id, TargetName, id);
            _access.TrySetTags(id, ["Captain"]);
            _implants.AddImplant(sTarget, "MindShieldImplant");

            Assert.That(_inventory.SpawnItemInSlot(SPlayer, "jumpsuit", "ClothingUniformJumpsuitColorGrey"));
            Assert.That(_inventory.TryGetSlotEntity(SPlayer, "jumpsuit", out var suit));
            ownSuit = suit!.Value;
        });

        var remote = await PlaceInHands("STDisguiseRemote");
        await Interact();
        await RunTicks(5);
        AssertDeleted(remote);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(SPlayer).EntityName, Is.EqualTo(ownName));
            Assert.That(_inventory.TryGetSlotEntity(SPlayer, "jumpsuit", out var suit));
            Assert.That(suit, Is.EqualTo(ownSuit));

            SEntMan.EventBus.RaiseLocalEvent(SPlayer, new DisguiseToggleActionEvent());
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(SPlayer).EntityName, Is.EqualTo(TargetName));
            Assert.That(_inventory.TryGetSlotEntity(SPlayer, "jumpsuit", out var suit));
            Assert.That(suit, Is.Not.EqualTo(ownSuit));
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(suit!.Value).EntityPrototype?.ID, Is.EqualTo(TargetSuit));

            Assert.That(_idCard.TryFindIdCard(SPlayer, out var id));
            Assert.That(id.Comp.FullName, Is.EqualTo(TargetName));
            Assert.That(SEntMan.GetComponent<AccessComponent>(id).Tags, Does.Contain(new ProtoId<AccessLevelPrototype>("Captain")));

            Assert.That(SEntMan.GetComponent<JobStatusComponent>(SPlayer).JobStatusIcon, Is.EqualTo(id.Comp.JobIcon));
            _mindShield.GetMindshieldStatus(SPlayer, out var shielded, out var shieldVisible);
            Assert.That(shieldVisible, Is.True);
            Assert.That(shielded, Is.False, "Щит разума должен быть только значком, без защиты.");

            Assert.That(_inventory.TryGetSlotEntity(sTarget, "jumpsuit", out _));
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(sTarget).EntityName, Is.EqualTo(TargetName));

            SEntMan.EventBus.RaiseLocalEvent(SPlayer, new DisguiseToggleActionEvent());
        });
        await RunTicks(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(SPlayer).EntityName, Is.EqualTo(ownName));
            Assert.That(_inventory.TryGetSlotEntity(SPlayer, "jumpsuit", out var suit));
            Assert.That(suit, Is.EqualTo(ownSuit));
            Assert.That(_inventory.TryGetSlotEntity(SPlayer, "id", out _), Is.False);
            _mindShield.GetMindshieldStatus(SPlayer, out _, out var shieldVisible);
            Assert.That(shieldVisible, Is.False);

            SEntMan.EventBus.RaiseLocalEvent(SPlayer, new DisguiseToggleActionEvent());
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(SPlayer).EntityName, Is.EqualTo(TargetName));
            Assert.That(_idCard.TryFindIdCard(SPlayer, out var id));
            Assert.That(id.Comp.FullName, Is.EqualTo(TargetName));
        });
    }

    [Test]
    [Description("При перевоплощении в другую расу наложения урона перекрашиваются в её цвет крови и обратно.")]
    public async Task DamageOverlayColorFollowsSpeciesTest()
    {
        var target = await SpawnTarget("MobSlimePerson");
        await PlaceInHands("STDisguiseRemote");
        await Interact();
        await RunTicks(5);

        Assert.That(GetChestBruteColor(), Is.EqualTo(Color.FromHex("#FF0000")));

        await Server.WaitPost(() => SEntMan.EventBus.RaiseLocalEvent(SPlayer, new DisguiseToggleActionEvent()));
        await RunTicks(5);
        Assert.That(GetChestBruteColor(), Is.EqualTo(Color.FromHex("#2cf274")), "В облике слайма кровь должна быть слаймовой.");

        await Server.WaitPost(() => SEntMan.EventBus.RaiseLocalEvent(SPlayer, new DisguiseToggleActionEvent()));
        await RunTicks(5);
        Assert.That(GetChestBruteColor(), Is.EqualTo(Color.FromHex("#FF0000")), "В своём облике кровь должна вернуться.");
    }

    private Color GetChestBruteColor()
    {
        var sprite = CEntMan.GetComponent<SpriteComponent>(CPlayer);
        Assert.That(_sprite.LayerMapTryGet((CPlayer, sprite), "ChestBrute", out var index, false), "Нет слоя наложения урона на груди.");
        return sprite[index].Color;
    }
}
