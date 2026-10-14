#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Verbs;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory;
using Content.Shared._Stories.Heretic.Cloak;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticCloakTest : GameTest
{
    private static readonly EntProtoId ActionProto = "STActionHereticShadowCloak";

    [Test]
    public async Task CloakHidesAndBreaksOnDamage()
    {
        var testMap = await Pair.CreateTestMap();
        var damageable = Server.System<DamageableSystem>();
        var heretic = EntityUid.Invalid;
        var observer = EntityUid.Invalid;
        var realName = string.Empty;
        var baseSpeed = 0f;

        await Server.WaitAssertion(() =>
        {
            heretic = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            observer = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            SEntMan.AddComponent<StoriesHereticComponent>(heretic);
            // Поднимаем порог крита, чтобы плащ срывался именно уроном, а не сменой состояния.
            var thresholds = Server.System<MobThresholdSystem>();
            thresholds.SetMobStateThreshold(heretic, 1000, MobState.Critical);
            thresholds.SetMobStateThreshold(heretic, 2000, MobState.Dead);
            realName = SEntMan.GetComponent<MetaDataComponent>(heretic).EntityName;
            // С ID-картой личность без плаща — имя с карты; плащ должен скрыть и его.
            var id = SSpawnAtPosition("PassengerIDCard", testMap.GridCoords);
            Assert.That(Server.System<SharedIdCardSystem>().TryChangeFullName(id, realName));
            Assert.That(Server.System<InventorySystem>().TryEquip(heretic, id, "id", silent: true, force: true));
            // Маска с блокатором личности вместе с ID: отмена маски не должна вернуть имя с карты.
            var mask = SSpawnAtPosition("ClothingMaskGas", testMap.GridCoords);
            Assert.That(Server.System<InventorySystem>().TryEquip(heretic, mask, "mask", silent: true, force: true));
            baseSpeed = SEntMan.GetComponent<MovementSpeedModifierComponent>(heretic).CurrentSprintSpeed;

            var ev = new StoriesHereticShadowCloakEvent { Performer = heretic };
            SEntMan.EventBus.RaiseLocalEvent(heretic, ev);
            Assert.That(ev.Handled, Is.True);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<StoriesHereticShadowCloakComponent>(heretic));
            Assert.That(Identity.Name(heretic, SEntMan, observer),
                Is.EqualTo(Server.ResolveDependency<ILocalizationManager>().GetString("stories-heretic-cloak-voice")), "Плащ не скрыл имя с ID-карты");
            Assert.That(SEntMan.GetComponent<MovementSpeedModifierComponent>(heretic).CurrentSprintSpeed, Is.GreaterThan(baseSpeed));
            Assert.That(HasIdExamineVerb(heretic, observer), Is.False, "Осмотр «ID» выдаёт имя и должность под плащом");

            damageable.TryChangeDamage(heretic, new DamageSpecifier { DamageDict = new() { { "Blunt", 30 } } }, true);
            damageable.TryChangeDamage(heretic, new DamageSpecifier { DamageDict = new() { { "Blunt", 70 } } }, true);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<StoriesHereticShadowCloakComponent>(heretic), Is.False, "Плащ не сорван уроном");
            Assert.That(Identity.Name(heretic, SEntMan, observer), Is.Not.EqualTo(Server.ResolveDependency<ILocalizationManager>().GetString("stories-heretic-cloak-voice")));
            Assert.That(SEntMan.GetComponent<MobStateComponent>(heretic).CurrentState, Is.EqualTo(MobState.Alive));
            Assert.That(HasIdExamineVerb(heretic, observer), Is.True, "Осмотр «ID» не вернулся после плаща");
        });
    }

    private bool HasIdExamineVerb(EntityUid target, EntityUid user)
    {
        var text = Server.ResolveDependency<ILocalizationManager>().GetString("id-examinable-component-verb-text");
        return Server.System<SharedVerbSystem>().GetLocalVerbs(target, user, typeof(ExamineVerb), force: true)
            .Any(verb => verb.Text == text);
    }

    [Test]
    public async Task ManualRemovalKeepsRemainingCooldown()
    {
        var testMap = await Pair.CreateTestMap();
        var actions = Server.System<SharedActionsSystem>();
        var timing = Server.ResolveDependency<IGameTiming>();
        var heretic = EntityUid.Invalid;
        Entity<ActionComponent> action = default;

        await Server.WaitAssertion(() =>
        {
            heretic = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            SEntMan.AddComponent<StoriesHereticComponent>(heretic);
            var uid = actions.AddAction(heretic, ActionProto);
            Assert.That(uid, Is.Not.Null);
            action = (uid!.Value, SEntMan.GetComponent<ActionComponent>(uid.Value));

            actions.PerformAction(heretic, action);
            Assert.That(SEntMan.HasComponent<StoriesHereticShadowCloakComponent>(heretic));
            SEntMan.GetComponent<StoriesHereticShadowCloakComponent>(heretic).EndTime = timing.CurTime + TimeSpan.FromSeconds(120);

            actions.PerformAction(heretic, action);
        });

        await Pair.RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<StoriesHereticShadowCloakComponent>(heretic), Is.False);
            var cd = SEntMan.GetComponent<ActionComponent>(action).Cooldown;
            Assert.That(cd, Is.Not.Null);
            Assert.That((cd!.Value.End - timing.CurTime).TotalSeconds, Is.GreaterThan(100));
        });
    }
}
