#pragma warning disable RA0002
using System.Linq;
using Content.IntegrationTests.Tests.GameRules;
using Content.Server._Stories.Heretic;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.GameTicking;
using Content.Server._Stories.Heretic.Summons;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Antag;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

/// <summary>
/// Сквозная проверка целей еретика Плоти: настоящий раунд, правило STHeretic, призывы через систему призрачных ролей.
/// </summary>
[TestFixture]
public sealed class HereticObjectivesTest : AntagTest
{
    private static readonly ProtoId<AntagSpecifierPrototype> Specifier = "STHereticSpecifier";
    private const string Knowledge = "STHereticObjectiveKnowledge";
    private const string Sacrifice = "STHereticObjectiveSacrifice";
    private const string HeadSacrifice = "STHereticObjectiveHeadSacrifice";
    private const string Summon = "STHereticObjectiveSummon";
    private const string Ascension = "STHereticKnowledgeFleshAscension";

    [Test]
    public async Task FleshObjectivesCountEndToEnd()
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

        // Двое игроков-призраков для призывов и третий — душа гуля.
        var sessions = await Server.AddDummySessions(3);
        var (first, second, ghoulSoul) = (sessions[0], sessions[1], sessions[2]);
        var testMap = await Pair.CreateTestMap();
        await Pair.RunTicksSync(5);

        var heretics = Server.System<StoriesHereticSystem>();
        var objectives = Server.System<SharedObjectivesSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var ghostRoles = Server.System<GhostRoleSystem>();
        var damageable = Server.System<DamageableSystem>();
        var loc = Server.ResolveDependency<ILocalizationManager>();
        var heretic = default(Entity<StoriesHereticComponent>);
        var mind = default(Entity<MindComponent>);
        var rune = EntityUid.Invalid;

        EntityUid? Find(string proto) => mind.Comp.Objectives
            .Cast<EntityUid?>()
            .FirstOrDefault(o => SEntMan.GetComponent<MetaDataComponent>(o!.Value).EntityPrototype?.ID == proto);

        float Progress(string proto) => objectives.GetProgress(Find(proto)!.Value, mind) ?? -1f;

        await Server.WaitAssertion(() =>
        {
            var player = Pair.Player!.AttachedEntity!.Value;
            // Чистая карта: ритуал не должен подхватить предметы станции.
            Server.System<SharedTransformSystem>().SetCoordinates(player, testMap.GridCoords);
            heretic = (player, SEntMan.GetComponent<StoriesHereticComponent>(player));
            Assert.That(SMind.TryGetMind(Pair.Player!, out var mindId, out var mindComp));
            mind = (mindId, mindComp!);

            Assert.That(Find(Knowledge), Is.Not.Null, "Нет цели знаний");
            Assert.That(Find(Sacrifice), Is.Not.Null, "Нет цели жертв");
            Assert.That(Find(HeadSacrifice), Is.Not.Null, "Нет цели жертвы главы");
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(Find(Sacrifice)!.Value).EntityName,
                Is.EqualTo(Ru ? "Принести в жертву 4 цели." : "Sacrifice 4 targets."), "Склонение в заголовке цели жертв");
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(Find(Knowledge)!.Value).EntityName,
                Is.EqualTo(Ru ? "Изучить 6 знаний Мансуса." : "Learn 6 Mansus knowledges."));
            Assert.That(Find(Summon), Is.Null, "Цель призывов до выбора пути");
            Assert.That(Progress(Knowledge), Is.Zero);

            heretic.Comp.Points = 30;
            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFleshRoot"), Is.True);
            var summon = Find(Summon);
            Assert.That(summon, Is.Not.Null, "Принцип Голода не добавил цель призывов");
            var title = SEntMan.GetComponent<MetaDataComponent>(summon!.Value).EntityName;
            Assert.That(title, Is.EqualTo(loc.GetString("stories-heretic-objective-summon", ("count", 2))));
            Assert.That(title, Is.EqualTo(Ru ? "Призвать монстров Мансуса: 2." : "Summon 2 Mansus monsters."));
            Assert.That(mind.Comp.Objectives.Count(o => o == summon), Is.EqualTo(1));
            Assert.That(Progress(Summon), Is.Zero);
            Assert.That(Progress(Knowledge), Is.EqualTo(1f / heretic.Comp.RequiredKnowledge).Within(0.001f));

            foreach (var id in new[] { "Grasp", "Imperfect", "Mark", "Knitting", "Raw", "BleedingSteel", "Lonely" })
            {
                var before = Progress(Knowledge);
                Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeFlesh" + id), Is.True, id);
                if (heretic.Comp.LearnedCount <= heretic.Comp.RequiredKnowledge)
                    Assert.That(Progress(Knowledge), Is.GreaterThan(before), $"Знание {id} не продвинуло цель");
            }

            Assert.That(objectives.IsCompleted(Find(Knowledge)!.Value, mind), Is.True, "Цель знаний не выполнена");

            // Остальные цели закрываем состоянием: проверяется только влияние призывов на Вознесение.
            heretic.Comp.SacrificeCount = heretic.Comp.RequiredSacrifices;
            heretic.Comp.HeadSacrificeCount = heretic.Comp.RequiredHeadSacrifices;
            Assert.That(objectives.IsCompleted(Find(Sacrifice)!.Value, mind), Is.True);
            Assert.That(objectives.IsCompleted(Find(HeadSacrifice)!.Value, mind), Is.True);
            Assert.That(heretics.CanLearn(heretic, Ascension, out var reason), Is.False, "Вознесение без призывов");
            Assert.That(reason, Is.EqualTo(loc.GetString("stories-heretic-learn-ascension-objectives")));

            rune = SSpawnAtPosition("STHereticRune", testMap.GridCoords);
        });

        // Первый призыв занимает первый игрок.
        await TakeStalker(heretic, rune, first, ghostRoles);
        await Server.WaitAssertion(() =>
        {
            Assert.That(heretic.Comp.SummonCount, Is.EqualTo(1));
            Assert.That(Progress(Summon), Is.EqualTo(0.5f).Within(0.001f));
        });

        // Тот же игрок во втором призыве цель не продвигает.
        await TakeStalker(heretic, rune, first, ghostRoles);
        await Server.WaitAssertion(() =>
        {
            Assert.That(heretic.Comp.SummonCount, Is.EqualTo(1), "Один игрок засчитан дважды");
            Assert.That(Progress(Summon), Is.EqualTo(0.5f).Within(0.001f));

            // Гуль Хватки — не призыв.
            var corpse = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            SMind.TransferTo(SMind.CreateMind(ghoulSoul.UserId), corpse);
            damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            var ev = new StoriesHereticGraspHitEvent(heretic, corpse);
            SEntMan.EventBus.RaiseLocalEvent(heretic, ref ev);
            Assert.That(heretics.IsServant(corpse, heretic), Is.True, "Гуль не поднялся");
            Assert.That(heretic.Comp.SummonCount, Is.EqualTo(1), "Гуль засчитан как призыв");
            Assert.That(Progress(Summon), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(heretics.CanLearn(heretic, Ascension, out _), Is.False);
        });

        // Второй игрок — второй призыв.
        await TakeStalker(heretic, rune, second, ghostRoles);
        await Server.WaitAssertion(() =>
        {
            Assert.That(heretic.Comp.SummonCount, Is.EqualTo(2));
            Assert.That(Progress(Summon), Is.EqualTo(1f).Within(0.001f));
            Assert.That(objectives.IsCompleted(Find(Summon)!.Value, mind), Is.True);
            Assert.That(heretics.CanLearn(heretic, Ascension, out var reason), Is.True, $"Вознесение закрыто: {reason}");

            var ev = new RoundEndTextAppendEvent();
            SEntMan.EventBus.RaiseEvent(EventSource.Local, ev);
            Assert.That(ev.Text, Does.Contain(loc.GetString("stories-heretic-round-end-summons", ("count", 2), ("required", 2))));
            Assert.That(ev.Text, Does.Contain(Ru ? "2 из 2" : "2 of 2"), "В итогах раунда нет счёта призывов");

            // Вознесение и Владыка Ночи: разум уходит из тела еретика, цели и итоги читаются со снимка роли.
            Assert.That(heretics.TryLearn(heretic, Ascension), Is.True);
            for (var i = 0; i < 4; i++)
            {
                var corpse = SSpawnAtPosition("MobHuman", testMap.GridCoords);
                damageable.TryChangeDamage(corpse, new DamageSpecifier { DamageDict = new() { { "Blunt", 250 } } }, true);
            }

            Assert.That(rituals.TryPerform(heretic, rune), Is.True, "Вознесение не состоялось");
            Assert.That(heretic.Comp.Ascended, Is.True);

            var actions = Server.System<SharedActionsSystem>();
            var shed = actions.GetActions(heretic)
                .First(a => SEntMan.GetComponent<MetaDataComponent>(a).EntityPrototype?.ID == "STActionHereticShedForm");
            actions.PerformAction(heretic.Owner, shed, predicted: false);
            Assert.That(mind.Comp.OwnedEntity, Is.Not.EqualTo(heretic.Owner), "Владыка Ночи не появился");

            foreach (var proto in new[] { Knowledge, Sacrifice, HeadSacrifice, Summon })
                Assert.That(objectives.IsCompleted(Find(proto)!.Value, mind), Is.True, $"{proto} не выполнена в облике Владыки");

            ev = new RoundEndTextAppendEvent();
            SEntMan.EventBus.RaiseEvent(EventSource.Local, ev);
            Assert.That(ev.Text, Does.Contain(Ru ? "вознёсся" : "ascended"), "Итоги не знают о Вознесении");
            Assert.That(ev.Text, Does.Contain(Ru ? "2 из 2" : "2 of 2"));
        });
    }

    private bool Ru => Server.ResolveDependency<ILocalizationManager>().DefaultCulture?.Name.StartsWith("ru") == true;

    /// <summary>
    /// Одинокий ритуал на руне, затем роль Сталкера берёт <paramref name="session"/> через систему призрачных ролей.
    /// </summary>
    private async Task TakeStalker(Entity<StoriesHereticComponent> heretic, EntityUid rune, ICommonSession session, GhostRoleSystem ghostRoles)
    {
        await Server.WaitAssertion(() =>
        {
            var coords = SEntMan.GetComponent<TransformComponent>(rune).Coordinates;
            for (var i = 0; i < 3; i++)
                SSpawnAtPosition("FoodMeat", coords);
            SSpawnAtPosition("Pen", coords);
            SSpawnAtPosition("Paper", coords);
            Assert.That(Server.System<StoriesHereticRitualSystem>().TryPerform(heretic, rune), Is.True, "Одинокий ритуал не сработал");

            EntityUid? stalker = null;
            var query = SEntMan.EntityQueryEnumerator<StoriesHereticSummonComponent, GhostRoleComponent>();
            while (query.MoveNext(out var uid, out var summon, out _))
            {
                if (!summon.Bound)
                    stalker = uid;
            }

            Assert.That(stalker, Is.Not.Null, "Сталкер не призван");
            var role = SEntMan.GetComponent<GhostRoleComponent>(stalker!.Value);
            Assert.That(ghostRoles.Takeover(session, role.Identifier), Is.True, "Роль не взята");
            Assert.That(session.AttachedEntity, Is.EqualTo(stalker), "Игрок не в Сталкере");
        });

        // Поглощённое удаляется в конце тика — иначе следующий ритуал нашёл бы его снова.
        await Pair.RunTicksSync(1);
    }
}
