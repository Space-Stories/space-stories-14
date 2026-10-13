# Еретик: ядро + путь Ash. План реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Играбельный антагонист Еретик (Heretic) с ядром механик и путём Ash в форке Space Stories.

**Architecture:** Данные (знания, ритуалы) хранятся в прототипах сущностей с компонентами. Состоянием еретика владеет одна shared-система `StoriesHereticSystem` (partial-файлы), остальные системы работают через её API. Серверные части: правило раунда, жертвоприношение и Теневой мир, огненная логика Ash (поджог есть только на сервере). Клиент: окно дерева знаний (XAML) и видимость влияний только для еретика.

**Tech Stack:** C# / RobustToolbox ECS, YAML-прототипы, Fluent (`.ftl`), NUnit-интеграционные тесты (`GameTest`).

**Spec:** [docs/superpowers/specs/2026-10-13-heretic-core-design.md](../specs/2026-10-13-heretic-core-design.md)

## Global Constraints

- Все правила из [coding-standards.md](../../../coding-standards.md) обязательны.
- Новый код: `Content.Shared/_Stories/Heretic/`, `Content.Server/_Stories/Heretic/`, `Content.Client/_Stories/Heretic/`; YAML: `Resources/Prototypes/_Stories/Heretic/`.
- Классы систем и компонентов — с префиксом `Stories`. ID прототипов сущностей — с префиксом `ST`. Ключи локализации — с `stories-`.
- Новых **видов** прототипов не создавать (только `entity`, `tag`, `dataset`, `polymorph`, `damageModifierSet`, `antag`, `antagSpecifier`, `gamePreset`, `guideEntry` — уже существующие виды).
- `[DataField]` без явного имени. `var` везде, где возможно. Подписки через `Entity<T>`, методы `On*`. Зависимости вверху по алфавиту без слова «System». Константы — в полях компонентов. ID прототипов в коде — `static readonly ProtoId<T>` / `EntProtoId`.
- События — `record struct` с `[ByRefEvent]` и полем `Handled` / `Cancelled`, где это возможно. Классы — только когда нужно наследование (события действий, do-after).
- Поиск через `EntityLookupSystem` — только перегрузки с `HashSet`, хранящимся в `private readonly` поле системы.
- UI — через `.xaml`; данные окно берёт из компонента (без BUI-состояний); обновление через `AfterAutoHandleStateEvent` в `try/catch` с логированием.
- Изменения апстрим-файлов помечать `# Stories-Heretic` / `// Stories-Heretic`. В этом плане меняется только один апстрим-файл (`Resources/Prototypes/Guidebook/antagonist.yml`).
- Локализация: основной язык `ru-RU`, плюс `en-US`. Имена сущностей в ru-RU — в `Resources/Locale/ru-RU/ss14-ru/prototypes/_stories/heretic/*.ftl` (`ent-ID = ...`, `.desc = ...`), прочие ключи — в `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`.
- Спрайты — заглушки из существующих RSI (полный путь ниже в каждом YAML).

## Команды проверки

- Сборка: `dotnet build -c Debug` (из корня репозитория). Ожидается `0 Error(s)`.
- Тесты Еретика: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~Heretic"`.
- YAML-линтер: `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c Debug --no-build`. Ожидается выход с кодом 0.

## Карта файлов

### Content.Shared/_Stories/Heretic/
| Файл | Ответственность |
|---|---|
| `Components/StoriesHereticComponent.cs` | Состояние еретика |
| `Components/StoriesHereticKnowledgeComponent.cs` | Данные знания (на прототипе) |
| `Components/StoriesHereticRitualComponent.cs` | Данные ритуала + `StoriesHereticIngredient` + `StoriesHereticRitualEffect` |
| `Components/StoriesHereticRuneComponent.cs` | Метка руны |
| `Components/StoriesHereticInfluenceComponent.cs` | Влияние |
| `Components/StoriesHereticPiercedRealityComponent.cs` | Разорванная реальность |
| `Components/StoriesHereticFocusComponent.cs` | Предмет-фокус |
| `Components/StoriesHereticCodexComponent.cs` | Кодекс |
| `Components/StoriesHereticRequiresFocusComponent.cs` | Действие требует фокус |
| `Components/StoriesHereticBladeComponent.cs` | Клинок еретика |
| `Components/StoriesHereticMarkComponent.cs` | Метка на цели |
| `Components/StoriesHereticGraspMarkComponent.cs` | Хватка ставит метку |
| `Components/StoriesHereticGraspBlindComponent.cs` | Хватка ослепляет |
| `Components/StoriesHereticSacrificedComponent.cs` | Жертва |
| `Events/StoriesHereticActionEvents.cs` | События действий |
| `Events/StoriesHereticEvents.cs` | Внутренние события + do-after |
| `StoriesHereticUi.cs` | Ключ UI и сообщение покупки |
| `StoriesHereticSystem.cs` | Ядро: init, API состояния |
| `StoriesHereticSystem.Knowledge.cs` | Кэш знаний, `CanLearn` / `TryLearn` |
| `StoriesHereticSystem.Focus.cs` | Фокус, Кодекс |
| `StoriesHereticRitualSystem.cs` | Руны, сопоставление рецептов |
| `StoriesHereticGraspSystem.cs` | Хватка, метки, клинок, ответ жертв |
| `StoriesHereticInfluenceSystem.cs` | Влияния и разорванная реальность |

### Content.Server/_Stories/Heretic/
| Файл | Ответственность |
|---|---|
| `StoriesHereticRuleComponent.cs`, `StoriesHereticRuleSystem.cs` | Правило, влияния, итоги |
| `StoriesHereticObjectiveConditionComponent.cs`, `StoriesHereticObjectiveSystem.cs` | Цели |
| `StoriesHereticShadowRealmComponent.cs` | Пребывание в Теневом мире |
| `StoriesHereticSacrificeSystem.cs` | Цели, Живое сердце, жертва, переброс |
| `StoriesHereticSacrificeSystem.ShadowRealm.cs` | Генерация/загрузка Теневого мира |
| `Ash/StoriesHereticAshSystem.cs` | Метка, вулкан, перерождение, огненный клинок |
| `Ash/StoriesHereticAshSystem.Ascension.cs` | Вознесение Ash, кольцо и волна огня, Маска безумия |
| `Ash/StoriesHereticAshMarkComponent.cs` | Параметры метки Пепла |
| `Ash/StoriesHereticBladeFireComponent.cs` | Огненный клинок |
| `Ash/StoriesHereticMadnessMaskComponent.cs` | Маска безумия |
| `Ash/StoriesHereticFireRingComponent.cs` | Огненное кольцо |
| `Ash/StoriesHereticAshAscensionRitualComponent.cs` | Данные ритуала Вознесения |

Компоненты Ash серверные: клиент пропускает незнакомые ему компоненты в YAML (`IgnoreMissingComponents` в `Content.Client/Entry/EntryPoint.cs`).

### Content.Client/_Stories/Heretic/
| Файл | Ответственность |
|---|---|
| `StoriesHereticInfluenceVisibilitySystem.cs` | Влияния видны только еретику |
| `StoriesHereticKnowledgeUISystem.cs` | Обновление открытого окна |
| `UI/StoriesHereticKnowledgeBoundUserInterface.cs` | BUI: сборка узлов, покупка |
| `UI/StoriesHereticKnowledgeWindow.xaml(.cs)` | Окно |
| `UI/StoriesHereticKnowledgeTreeControl.cs` | Раскладка узлов и линии связей |
| `UI/StoriesHereticKnowledgeNode.cs` | Модель узла |

### Resources/
| Файл | Содержимое |
|---|---|
| `Prototypes/_Stories/Heretic/tags.yml` | Теги путей |
| `Prototypes/_Stories/Heretic/datasets.yml` | Теги ритуала знаний |
| `Prototypes/_Stories/Heretic/antag.yml` | `antag`, `antagSpecifier`, роль разума |
| `Prototypes/_Stories/Heretic/game_rules.yml` | Правило + режим |
| `Prototypes/_Stories/Heretic/objectives.yml` | Цели |
| `Prototypes/_Stories/Heretic/actions.yml`, `actions_ash.yml` | Действия |
| `Prototypes/_Stories/Heretic/knowledge.yml`, `knowledge_ash.yml` | Знания |
| `Prototypes/_Stories/Heretic/rituals.yml`, `rituals_ash.yml` | Ритуалы |
| `Prototypes/_Stories/Heretic/entities.yml`, `entities_ash.yml` | Руна, влияния, фокус, Кодекс, клинок, амулет, маска |
| `Prototypes/_Stories/Heretic/ash_misc.yml` | Полиморф рывка, модификатор урона |
| `Prototypes/_Stories/Heretic/guidebook.yml` | Гайдбук |
| `Prototypes/_Stories/secret_weights.yml` | +`STHeretic` (файл форка) |
| `Prototypes/Guidebook/antagonist.yml` | +`STHeretic` в дочерние (апстрим, с пометкой) |
| `Locale/ru-RU/_stories/heretic.ftl`, `Locale/en-US/_stories/heretic.ftl` | Строки |
| `Locale/ru-RU/ss14-ru/prototypes/_stories/heretic/entities.ftl` | Имена сущностей ru |
| `ServerInfo/_Stories/Heretic.xml` | Текст гайдбука |

### Content.IntegrationTests/Tests/_Stories/Heretic/
`HereticKnowledgeTest.cs`, `HereticRitualTest.cs`, `HereticRuleTest.cs`, `HereticSacrificeTest.cs`.

---

### Task 1: Модель данных и система знаний

**Files:**
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticKnowledgeComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticRitualComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Events/StoriesHereticActionEvents.cs`
- Create: `Content.Shared/_Stories/Heretic/Events/StoriesHereticEvents.cs`
- Create: `Content.Shared/_Stories/Heretic/StoriesHereticUi.cs`
- Create: `Content.Shared/_Stories/Heretic/StoriesHereticSystem.cs`
- Create: `Content.Shared/_Stories/Heretic/StoriesHereticSystem.Knowledge.cs`
- Create: `Resources/Prototypes/_Stories/Heretic/tags.yml`, `datasets.yml`, `knowledge.yml`, `actions.yml`
- Create: `Resources/Locale/ru-RU/_stories/heretic.ftl`, `Resources/Locale/en-US/_stories/heretic.ftl`
- Test: `Content.IntegrationTests/Tests/_Stories/Heretic/HereticKnowledgeTest.cs`

**Interfaces:**
- Produces:
  - `StoriesHereticComponent` (поля ниже).
  - `StoriesHereticSystem.CanLearn(Entity<StoriesHereticComponent> ent, EntProtoId id, out string? reason) : bool`
  - `StoriesHereticSystem.TryLearn(Entity<StoriesHereticComponent> ent, EntProtoId id) : bool`
  - `StoriesHereticSystem.TryGetKnowledge(EntProtoId id, [NotNullWhen(true)] out StoriesHereticKnowledgeComponent? knowledge) : bool`
  - `StoriesHereticSystem.AllKnowledge : IReadOnlyList<(EntProtoId Id, StoriesHereticKnowledgeComponent Comp)>`
  - `StoriesHereticSystem.AddPoints(Entity<StoriesHereticComponent> ent, int amount)`
  - `StoriesHereticSystem.ObjectivesComplete(StoriesHereticComponent comp) : bool`
  - `StoriesHereticSystem.RegisterSacrifice(Entity<StoriesHereticComponent> ent, EntityUid victim, bool head)`
  - `StoriesHereticSystem.SetTargets(Entity<StoriesHereticComponent> ent, List<EntityUid> targets)`
  - `StoriesHereticSystem.CycleTrackedTarget(Entity<StoriesHereticComponent> ent) : EntityUid?`
  - `StoriesHereticSystem.RemoveRitual(Entity<StoriesHereticComponent> ent, EntProtoId ritual)`
  - `StoriesHereticSystem.LockAscension(Entity<StoriesHereticComponent> ent)`
  - `StoriesHereticSystem.SetAscended(Entity<StoriesHereticComponent> ent)`
  - `StoriesHereticSystem.MarkKnowledgeRitualUsed(Entity<StoriesHereticComponent> ent)`
  - `StoriesHereticSystem.TrackLimitedResult(Entity<StoriesHereticComponent> ent, EntProtoId ritual, EntityUid result)`
  - `StoriesHereticSystem.CountLimitedResults(Entity<StoriesHereticComponent> ent, EntProtoId ritual) : int`
  - Тег пути Ash: `STHereticPathAsh`. Знание-корень Ash появится в Task 9; в этой задаче тестовые знания определяются в самом тесте.

- [ ] **Step 1: Компоненты данных**

`Content.Shared/_Stories/Heretic/Components/StoriesHereticComponent.cs`:
```csharp
using Content.Shared.Dataset;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Состояние еретика. Изменяется только через API <see cref="StoriesHereticSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(StoriesHereticSystem))]
public sealed partial class StoriesHereticComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField, AutoNetworkedField]
    public int Points = 1;

    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype>? Path;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> Knowledge = new();

    /// <summary>
    /// Число купленных (не стартовых) знаний — для цели «изучить N знаний».
    /// </summary>
    [DataField, AutoNetworkedField]
    public int LearnedCount;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> Rituals = new();

    [DataField, AutoNetworkedField]
    public List<NetEntity> Targets = new();

    [DataField, AutoNetworkedField]
    public int TrackedTarget;

    [DataField, AutoNetworkedField]
    public int SacrificeCount;

    [DataField, AutoNetworkedField]
    public int HeadSacrificeCount;

    [DataField, AutoNetworkedField]
    public int RequiredKnowledge = 6;

    [DataField, AutoNetworkedField]
    public int RequiredSacrifices = 4;

    [DataField, AutoNetworkedField]
    public int RequiredHeadSacrifices = 1;

    [DataField]
    public int TargetCount = 5;

    [DataField, AutoNetworkedField]
    public bool Ascended;

    [DataField, AutoNetworkedField]
    public bool CanAscend = true;

    [DataField, AutoNetworkedField]
    public bool KnowledgeRitualUsed;

    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> KnowledgeRitualTags = new();

    [DataField]
    public ProtoId<DatasetPrototype> KnowledgeRitualDataset = "STHereticKnowledgeRitualTags";

    [DataField]
    public int KnowledgeRitualTagCount = 4;

    [DataField]
    public int KnowledgeRitualPoints = 4;

    [DataField]
    public int FeastOfOwlsPoints = 5;

    [DataField]
    public int SacrificePoints = 2;

    [DataField]
    public int HeadSacrificePoints = 3;

    [DataField]
    public TimeSpan PassivePointInterval = TimeSpan.FromMinutes(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPassivePoint;

    [DataField]
    public Dictionary<EntProtoId, List<EntityUid>> LimitedResults = new();

    [DataField]
    public List<EntityUid> GrantedActions = new();

    [DataField]
    public EntProtoId KnowledgeAction = "STActionHereticKnowledge";
}
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticKnowledgeComponent.cs`:
```csharp
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Данные одного знания. Живёт только на прототипах <c>STHereticKnowledge*</c>, сущности не спавнятся.
/// </summary>
[RegisterComponent]
public sealed partial class StoriesHereticKnowledgeComponent : Component
{
    [DataField]
    public int Cost = 1;

    /// <summary>
    /// Путь знания. <c>null</c> — общее или побочное знание.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype>? Path;

    [DataField]
    public int Stage;

    /// <summary>
    /// Нужно знать хотя бы одно из перечисленных (ИЛИ). Пустой список — без требований.
    /// </summary>
    [DataField]
    public List<EntProtoId<StoriesHereticKnowledgeComponent>> Requires = new();

    [DataField]
    public bool ChoosesPath;

    [DataField]
    public bool Starting;

    [DataField]
    public bool Ascension;

    [DataField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public List<EntProtoId<StoriesHereticRitualComponent>> Rituals = new();

    [DataField]
    public ComponentRegistry AddComponents = new();
}
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticRitualComponent.cs`:
```csharp
using Content.Shared.Body;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Рецепт ритуала. Живёт только на прототипах <c>STHereticRitual*</c>.
/// </summary>
[RegisterComponent]
public sealed partial class StoriesHereticRitualComponent : Component
{
    [DataField]
    public List<StoriesHereticIngredient> Ingredients = new();

    [DataField]
    public List<EntProtoId> Results = new();

    [DataField]
    public int? Limit;

    [DataField]
    public StoriesHereticRitualEffect Effect = StoriesHereticRitualEffect.None;
}

[DataDefinition]
public sealed partial class StoriesHereticIngredient
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public List<EntProtoId>? Prototypes;

    [DataField]
    public ProtoId<OrganCategoryPrototype>? OrganCategory;

    [DataField]
    public int Amount = 1;

    [DataField]
    public bool Consume = true;

    /// <summary>
    /// Название для сообщения «не хватает ...».
    /// </summary>
    [DataField(required: true)]
    public LocId Name;
}

public enum StoriesHereticRitualEffect : byte
{
    None,
    Sacrifice,
    Knowledge,
    FeastOfOwls,
    RerollTargets,
    Ascension,
}
```

- [ ] **Step 2: События и UI-типы**

`Content.Shared/_Stories/Heretic/Events/StoriesHereticActionEvents.cs`:
```csharp
using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Events;

public sealed partial class StoriesHereticOpenKnowledgeEvent : InstantActionEvent;

public sealed partial class StoriesHereticDrawRuneEvent : WorldTargetActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField]
    public float CodexSpeedMultiplier = 3f;

    [DataField]
    public EntProtoId Rune = "STHereticRune";
}

public sealed partial class StoriesHereticGraspEvent : EntityTargetActionEvent
{
    [DataField]
    public float StaminaDamage = 80f;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan SlurTime = TimeSpan.FromSeconds(5);
}

public sealed partial class StoriesHereticLivingHeartEvent : InstantActionEvent
{
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [DataField]
    public float NearDistance = 15f;

    [DataField]
    public float MediumDistance = 40f;
}

public sealed partial class StoriesHereticVolcanoBlastEvent : InstantActionEvent
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public int Bounces = 5;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float FireStacks = 3f;

    [DataField]
    public DamageSpecifier SelfHeal = new();
}

public sealed partial class StoriesHereticRebirthEvent : InstantActionEvent
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public DamageSpecifier HealPerTarget = new();

    [DataField]
    public DamageSpecifier FinishDamage = new();
}

public sealed partial class StoriesHereticFireRingEvent : InstantActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);
}

public sealed partial class StoriesHereticFireWaveEvent : InstantActionEvent
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public float HalfAngleDegrees = 30f;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float FireStacks = 5f;
}
```

`Content.Shared/_Stories/Heretic/Events/StoriesHereticEvents.cs`:
```csharp
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.DoAfter;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Heretic.Events;

/// <summary>
/// Поднимается на еретике после выдачи знания.
/// </summary>
[ByRefEvent]
public readonly record struct StoriesHereticKnowledgeLearnedEvent(EntProtoId Knowledge);

/// <summary>
/// Широковещательное событие для ритуалов с <see cref="StoriesHereticRitualEffect"/> (кроме Knowledge и FeastOfOwls).
/// Обработчик проверяет условия, при успехе добавляет поглощаемые сущности в <see cref="Consumed"/>
/// и ставит <see cref="Success"/>; при неудаче — <see cref="FailReason"/>.
/// </summary>
[ByRefEvent]
public record struct StoriesHereticRitualEffectEvent(
    Entity<StoriesHereticComponent> Heretic,
    EntityUid Rune,
    EntProtoId Ritual,
    StoriesHereticRitualEffect Effect,
    HashSet<EntityUid> Nearby,
    List<EntityUid> Consumed)
{
    public bool Handled;
    public bool Success;
    public string? FailReason;
}

/// <summary>
/// Поднимается на еретике после попадания Хваткой по существу.
/// </summary>
[ByRefEvent]
public readonly record struct StoriesHereticGraspHitEvent(EntityUid Heretic, EntityUid Target);

/// <summary>
/// Широковещательное: удар клинком по цели с меткой своего пути.
/// </summary>
[ByRefEvent]
public readonly record struct StoriesHereticMarkTriggeredEvent(EntityUid Heretic, EntityUid Target, ProtoId<TagPrototype> Path, int Hops);

/// <summary>
/// Поднимается на еретике при попадании клинком еретика.
/// </summary>
[ByRefEvent]
public readonly record struct StoriesHereticBladeHitEvent(EntityUid Heretic, EntityUid Target, EntityUid Blade);

[Serializable, NetSerializable]
public sealed partial class StoriesHereticDrawRuneDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    [DataField]
    public string Rune = string.Empty;

    private StoriesHereticDrawRuneDoAfterEvent()
    {
    }

    public StoriesHereticDrawRuneDoAfterEvent(NetCoordinates coordinates, string rune)
    {
        Coordinates = coordinates;
        Rune = rune;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class StoriesHereticHarvestDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StoriesHereticBladeBreakDoAfterEvent : SimpleDoAfterEvent;
```

`Content.Shared/_Stories/Heretic/StoriesHereticUi.cs`:
```csharp
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Heretic;

[Serializable, NetSerializable]
public enum StoriesHereticKnowledgeUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class StoriesHereticLearnMessage(string knowledge) : BoundUserInterfaceMessage
{
    public readonly string Knowledge = knowledge;
}
```

- [ ] **Step 3: Ядро системы**

`Content.Shared/_Stories/Heretic/StoriesHereticSystem.cs`:
```csharp
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Dataset;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Stories.Heretic;

/// <summary>
/// Единственный владелец <see cref="StoriesHereticComponent"/>. Остальные системы еретика используют его API.
/// </summary>
public sealed partial class StoriesHereticSystem : EntitySystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private const string KnowledgeBuiType = "StoriesHereticKnowledgeBoundUserInterface";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StoriesHereticComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticOpenKnowledgeEvent>(OnOpenKnowledge);

        InitializeKnowledge();
        InitializeFocus();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextPassivePoint)
                continue;

            comp.NextPassivePoint = now + comp.PassivePointInterval;
            AddPoints((uid, comp), 1);
        }
    }

    private void OnMapInit(Entity<StoriesHereticComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextPassivePoint = _timing.CurTime + ent.Comp.PassivePointInterval;

        if (_actions.AddAction(ent, ent.Comp.KnowledgeAction) is { } knowledgeAction)
            ent.Comp.GrantedActions.Add(knowledgeAction);

        EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi(ent.Owner, StoriesHereticKnowledgeUiKey.Key, new InterfaceData(KnowledgeBuiType));

        foreach (var (id, knowledge) in AllKnowledge)
        {
            if (knowledge.Starting)
                Grant(ent, id, knowledge);
        }

        PickKnowledgeRitualTags(ent);
        Dirty(ent);
    }

    /// <summary>
    /// Данные еретика видит только он сам: иначе по сети можно вычислить антагониста.
    /// </summary>
    private void OnGetStateAttempt(Entity<StoriesHereticComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Player?.AttachedEntity != ent.Owner)
            args.Cancelled = true;
    }

    private void OnOpenKnowledge(Entity<StoriesHereticComponent> ent, ref StoriesHereticOpenKnowledgeEvent args)
    {
        if (args.Handled)
            return;

        _ui.TryToggleUi(ent.Owner, StoriesHereticKnowledgeUiKey.Key, ent.Owner);
        args.Handled = true;
    }

    private void PickKnowledgeRitualTags(Entity<StoriesHereticComponent> ent)
    {
        if (_net.IsClient || !ProtoMan.TryIndex(ent.Comp.KnowledgeRitualDataset, out var dataset))
            return;

        var pool = new List<string>(dataset.Values);
        _random.Shuffle(pool);

        ent.Comp.KnowledgeRitualTags.Clear();
        for (var i = 0; i < Math.Min(ent.Comp.KnowledgeRitualTagCount, pool.Count); i++)
        {
            ent.Comp.KnowledgeRitualTags.Add(pool[i]);
        }
    }

    public void AddPoints(Entity<StoriesHereticComponent> ent, int amount)
    {
        ent.Comp.Points += amount;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("stories-heretic-points-gained", ("amount", amount)), ent, ent);
    }

    public bool ObjectivesComplete(StoriesHereticComponent comp)
    {
        return comp.LearnedCount >= comp.RequiredKnowledge
               && comp.SacrificeCount >= comp.RequiredSacrifices
               && comp.HeadSacrificeCount >= comp.RequiredHeadSacrifices;
    }

    public void SetTargets(Entity<StoriesHereticComponent> ent, List<EntityUid> targets)
    {
        ent.Comp.Targets.Clear();
        foreach (var target in targets)
        {
            ent.Comp.Targets.Add(GetNetEntity(target));
        }

        ent.Comp.TrackedTarget = 0;
        Dirty(ent);
    }

    /// <summary>
    /// Переключает Живое сердце на следующую существующую цель.
    /// </summary>
    public EntityUid? CycleTrackedTarget(Entity<StoriesHereticComponent> ent)
    {
        for (var i = 0; i < ent.Comp.Targets.Count; i++)
        {
            ent.Comp.TrackedTarget = (ent.Comp.TrackedTarget + 1) % ent.Comp.Targets.Count;
            if (TryGetEntity(ent.Comp.Targets[ent.Comp.TrackedTarget], out var target) && !TerminatingOrDeleted(target))
            {
                Dirty(ent);
                return target;
            }
        }

        return null;
    }

    public void RegisterSacrifice(Entity<StoriesHereticComponent> ent, EntityUid victim, bool head)
    {
        ent.Comp.Targets.Remove(GetNetEntity(victim));
        ent.Comp.SacrificeCount++;
        if (head)
            ent.Comp.HeadSacrificeCount++;

        Dirty(ent);
        AddPoints(ent, head ? ent.Comp.HeadSacrificePoints : ent.Comp.SacrificePoints);
    }

    public void RemoveRitual(Entity<StoriesHereticComponent> ent, EntProtoId ritual)
    {
        if (ent.Comp.Rituals.Remove(ritual))
            Dirty(ent);
    }

    public void LockAscension(Entity<StoriesHereticComponent> ent)
    {
        ent.Comp.CanAscend = false;
        Dirty(ent);
    }

    public void SetAscended(Entity<StoriesHereticComponent> ent)
    {
        ent.Comp.Ascended = true;
        Dirty(ent);
    }

    public void MarkKnowledgeRitualUsed(Entity<StoriesHereticComponent> ent)
    {
        ent.Comp.KnowledgeRitualUsed = true;
        Dirty(ent);
    }

    public void TrackLimitedResult(Entity<StoriesHereticComponent> ent, EntProtoId ritual, EntityUid result)
    {
        if (!ent.Comp.LimitedResults.TryGetValue(ritual, out var list))
        {
            list = new List<EntityUid>();
            ent.Comp.LimitedResults[ritual] = list;
        }

        list.Add(result);
    }

    public int CountLimitedResults(Entity<StoriesHereticComponent> ent, EntProtoId ritual)
    {
        if (!ent.Comp.LimitedResults.TryGetValue(ritual, out var list))
            return 0;

        list.RemoveAll(TerminatingOrDeleted);
        return list.Count;
    }
}
```

`Content.Shared/_Stories/Heretic/StoriesHereticSystem.Knowledge.cs`:
```csharp
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticSystem
{
    private readonly List<(EntProtoId Id, StoriesHereticKnowledgeComponent Comp)> _knowledge = new();

    public IReadOnlyList<(EntProtoId Id, StoriesHereticKnowledgeComponent Comp)> AllKnowledge => _knowledge;

    private void InitializeKnowledge()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        Subs.BuiEvents<StoriesHereticComponent>(StoriesHereticKnowledgeUiKey.Key,
            subs =>
            {
                subs.Event<StoriesHereticLearnMessage>(OnLearnMessage);
            });

        CacheKnowledge();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            CacheKnowledge();
    }

    private void CacheKnowledge()
    {
        _knowledge.Clear();
        foreach (var proto in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.TryComp<StoriesHereticKnowledgeComponent>(out var knowledge, _factory))
                _knowledge.Add((proto.ID, knowledge));
        }
    }

    private void OnLearnMessage(Entity<StoriesHereticComponent> ent, ref StoriesHereticLearnMessage args)
    {
        TryLearn(ent, args.Knowledge);
    }

    public bool TryGetKnowledge(EntProtoId id, [NotNullWhen(true)] out StoriesHereticKnowledgeComponent? knowledge)
    {
        foreach (var (knownId, comp) in _knowledge)
        {
            if (knownId != id)
                continue;

            knowledge = comp;
            return true;
        }

        knowledge = null;
        return false;
    }

    public bool CanLearn(Entity<StoriesHereticComponent> ent, EntProtoId id, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        if (!TryGetKnowledge(id, out var knowledge))
            reason = Loc.GetString("stories-heretic-learn-unknown");
        else if (ent.Comp.Knowledge.Contains(id))
            reason = Loc.GetString("stories-heretic-learn-known");
        else if (knowledge.ChoosesPath && ent.Comp.Path != null)
            reason = Loc.GetString("stories-heretic-learn-path-chosen");
        else if (!knowledge.ChoosesPath && knowledge.Path != null && knowledge.Path != ent.Comp.Path)
            reason = Loc.GetString("stories-heretic-learn-wrong-path");
        else if (knowledge.Requires.Count > 0 && !knowledge.Requires.Any(r => ent.Comp.Knowledge.Contains(r)))
            reason = Loc.GetString("stories-heretic-learn-requires");
        else if (knowledge.Ascension && !ent.Comp.CanAscend)
            reason = Loc.GetString("stories-heretic-learn-ascension-locked");
        else if (knowledge.Ascension && !ObjectivesComplete(ent.Comp))
            reason = Loc.GetString("stories-heretic-learn-ascension-objectives");
        else if (ent.Comp.Points < knowledge.Cost)
            reason = Loc.GetString("stories-heretic-learn-no-points");

        return reason == null;
    }

    public bool TryLearn(Entity<StoriesHereticComponent> ent, EntProtoId id)
    {
        if (!CanLearn(ent, id, out var reason))
        {
            _popup.PopupEntity(reason, ent, ent);
            return false;
        }

        TryGetKnowledge(id, out var knowledge);
        ent.Comp.Points -= knowledge!.Cost;
        ent.Comp.LearnedCount++;
        Grant(ent, id, knowledge);
        return true;
    }

    private void Grant(Entity<StoriesHereticComponent> ent, EntProtoId id, StoriesHereticKnowledgeComponent knowledge)
    {
        if (!ent.Comp.Knowledge.Add(id))
            return;

        if (knowledge.ChoosesPath)
            ent.Comp.Path = knowledge.Path;

        foreach (var action in knowledge.Actions)
        {
            if (_actions.AddAction(ent, action) is { } actionId)
                ent.Comp.GrantedActions.Add(actionId);
        }

        foreach (var ritual in knowledge.Rituals)
        {
            ent.Comp.Rituals.Add(ritual);
        }

        EntityManager.AddComponents(ent, knowledge.AddComponents, removeExisting: false);

        var ev = new StoriesHereticKnowledgeLearnedEvent(id);
        RaiseLocalEvent(ent, ref ev);
        Dirty(ent);
    }
}
```

Временная заглушка фокуса, чтобы проект собирался (полная реализация — Task 4), `Content.Shared/_Stories/Heretic/StoriesHereticSystem.Focus.cs`:
```csharp
namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticSystem
{
    private void InitializeFocus()
    {
    }
}
```

- [ ] **Step 4: YAML ядра (теги, датасет, действие окна, стартовые знания-заглушки)**

`Resources/Prototypes/_Stories/Heretic/tags.yml`:
```yaml
- type: Tag
  id: STHereticPathAsh
```

`Resources/Prototypes/_Stories/Heretic/datasets.yml`:
```yaml
- type: dataset
  id: STHereticKnowledgeRitualTags
  values:
  - Knife
  - Candle
  - Book
  - GlassShard
  - Matchstick
  - Write
  - Sheet
```

`Resources/Prototypes/_Stories/Heretic/actions.yml` (в этой задаче только действие окна; остальные действия добавляются в своих задачах):
```yaml
- type: entity
  id: STActionHereticKnowledge
  name: Mansus knowledge
  description: Open the tree of forbidden knowledge.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    checkCanInteract: false
  - type: Sprite
    sprite: Objects/Magic/magicactions.rsi
    layers:
    - state: spell_default
  - type: InstantAction
    event: !type:StoriesHereticOpenKnowledgeEvent
```

`Resources/Prototypes/_Stories/Heretic/knowledge.yml` (базовый родитель; стартовые знания с наградами добавляются в задачах 3–8):
```yaml
- type: entity
  abstract: true
  id: STHereticKnowledgeBase
  categories: [ HideSpawnMenu ]
  components:
  - type: StoriesHereticKnowledge
```

- [ ] **Step 5: Локализация ядра**

`Resources/Locale/ru-RU/_stories/heretic.ftl`:
```ftl
stories-heretic-points-gained = Вы получаете знания Мансуса: +{ $amount }.
stories-heretic-learn-unknown = Такого знания не существует.
stories-heretic-learn-known = Вы уже обладаете этим знанием.
stories-heretic-learn-path-chosen = Вы уже ступили на свой путь.
stories-heretic-learn-wrong-path = Это знание чуждо вашему пути.
stories-heretic-learn-requires = Сначала постигните предшествующее знание.
stories-heretic-learn-ascension-locked = Пир сов закрыл для вас путь Вознесения.
stories-heretic-learn-ascension-objectives = Сначала выполните свои цели.
stories-heretic-learn-no-points = Недостаточно знаний Мансуса.
```

`Resources/Locale/en-US/_stories/heretic.ftl`:
```ftl
stories-heretic-points-gained = You gain knowledge of the Mansus: +{ $amount }.
stories-heretic-learn-unknown = No such knowledge exists.
stories-heretic-learn-known = You already possess this knowledge.
stories-heretic-learn-path-chosen = You have already chosen your path.
stories-heretic-learn-wrong-path = This knowledge is foreign to your path.
stories-heretic-learn-requires = Learn the preceding knowledge first.
stories-heretic-learn-ascension-locked = The Feast of Owls has closed the path of Ascension.
stories-heretic-learn-ascension-objectives = Complete your objectives first.
stories-heretic-learn-no-points = Not enough knowledge of the Mansus.
```

- [ ] **Step 6: Падающий тест знаний**

`Content.IntegrationTests/Tests/_Stories/Heretic/HereticKnowledgeTest.cs`:
```csharp
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticKnowledgeTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: Tag
  id: STTestHereticPathA

- type: Tag
  id: STTestHereticPathB

- type: entity
  id: STTestHereticKnowledgeStart
  components:
  - type: StoriesHereticKnowledge
    starting: true
    cost: 0

- type: entity
  id: STTestHereticKnowledgeRootA
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathA
    choosesPath: true
    stage: 1

- type: entity
  id: STTestHereticKnowledgeRootB
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathB
    choosesPath: true
    stage: 1

- type: entity
  id: STTestHereticKnowledgeA2
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathA
    stage: 2
    requires: [ STTestHereticKnowledgeRootA ]

- type: entity
  id: STTestHereticKnowledgeAscend
  components:
  - type: StoriesHereticKnowledge
    path: STTestHereticPathA
    stage: 3
    cost: 0
    ascension: true
";

    [Test]
    public async Task LearningRules()
    {
        var testMap = await Pair.CreateTestMap();
        var sys = Server.System<StoriesHereticSystem>();

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var ent = new Entity<StoriesHereticComponent>(uid, comp);

            // Стартовое знание выдано бесплатно и не считается купленным.
            Assert.That(comp.Knowledge, Does.Contain((EntProtoId) "STTestHereticKnowledgeStart"));
            Assert.That(comp.LearnedCount, Is.Zero);
            Assert.That(comp.Points, Is.EqualTo(1));

            // Без корня пути следующая ступень недоступна.
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeA2"), Is.False);

            // Выбор пути списывает очко и фиксирует путь.
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeRootA"), Is.True);
            Assert.That(comp.Points, Is.Zero);
            Assert.That(comp.Path?.Id, Is.EqualTo("STTestHereticPathA"));
            Assert.That(comp.LearnedCount, Is.EqualTo(1));

            // Второй корень закрыт.
            sys.AddPoints(ent, 5);
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeRootB"), Is.False);

            // Следующая ступень своего пути доступна.
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeA2"), Is.True);

            // Вознесение без выполненных целей недоступно.
            Assert.That(sys.TryLearn(ent, "STTestHereticKnowledgeAscend"), Is.False);
        });
    }
}
```

- [ ] **Step 7: Сборка и тест**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~HereticKnowledgeTest"`
Expected: PASS. Тестовые прототипы (`[TestPrototypes]`) загружаются до инициализации систем, поэтому `CacheKnowledge` их видит.

- [ ] **Step 8: Коммит**

```bash
git add Content.Shared/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Locale/ru-RU/_stories/heretic.ftl Resources/Locale/en-US/_stories/heretic.ftl Content.IntegrationTests/Tests/_Stories/Heretic
git commit -m "feat(heretic): data model and knowledge system"
```

---

### Task 2: Правило раунда, роль, цели, режим

**Files:**
- Create: `Content.Server/_Stories/Heretic/StoriesHereticRuleComponent.cs`
- Create: `Content.Server/_Stories/Heretic/StoriesHereticRuleSystem.cs`
- Create: `Content.Server/_Stories/Heretic/StoriesHereticObjectiveConditionComponent.cs`
- Create: `Content.Server/_Stories/Heretic/StoriesHereticObjectiveSystem.cs`
- Create: `Resources/Prototypes/_Stories/Heretic/antag.yml`, `game_rules.yml`, `objectives.yml`
- Modify: `Resources/Prototypes/_Stories/secret_weights.yml` (добавить вес)
- Modify: `Resources/Locale/ru-RU/_stories/heretic.ftl`, `Resources/Locale/en-US/_stories/heretic.ftl`
- Test: `Content.IntegrationTests/Tests/_Stories/Heretic/HereticRuleTest.cs`

**Interfaces:**
- Consumes: `StoriesHereticComponent`, `StoriesHereticSystem.ObjectivesComplete` (Task 1).
- Produces:
  - `StoriesHereticRuleComponent` с полями `BaseInfluences`, `InfluencesPerHeretic`, `InfluencePrototype`, `InfluencesSpawned`, `ShadowRealmMap`.
  - `StoriesHereticRuleSystem.TryGetRandomStationCoordinates(out EntityCoordinates coords) : bool` — для возврата жертв (Task 6) и телепорта клинка (Task 4).
  - Выдачу целей для жертв правилу добавляет Task 6 (Step 4).

- [ ] **Step 1: Падающий тест правила**

`Content.IntegrationTests/Tests/_Stories/Heretic/HereticRuleTest.cs`:
```csharp
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticRuleTest : GameTest
{
    private const string RuleId = "STHeretic";
    private const string AntagId = "STHeretic";

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    [Test]
    public async Task RuleCreatesHeretic()
    {
        var pair = Pair;
        var server = pair.Server;
        var ticker = server.System<GameTicker>();
        var mindSys = server.System<MindSystem>();
        var roleSys = server.System<RoleSystem>();

        var minPlayers = 1;
        await server.WaitAssertion(() =>
        {
            var proto = SProtoMan.Index<EntityPrototype>(RuleId);
            Assert.That(proto.TryComp<GameRuleComponent>(out var rule, server.ResolveDependency<IComponentFactory>()));
            minPlayers = rule!.MinPlayers;
        });

        await pair.Server.AddDummySessions(minPlayers);
        await pair.RunTicksSync(5);
        await pair.SetAntagPreference(AntagId, true);

        await server.WaitPost(() =>
        {
            var ruleEnt = ticker.AddGameRule(RuleId);
            ticker.ToggleReadyAll(true);
            ticker.StartRound();
            ticker.StartGameRule(ruleEnt);
        });
        await pair.RunTicksSync(10);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        var player = pair.Player!.AttachedEntity!.Value;

        await server.WaitAssertion(() =>
        {
            var mind = mindSys.GetMind(player)!.Value;
            Assert.That(roleSys.MindIsAntagonist(mind));
            Assert.That(SEntMan.TryGetComponent<StoriesHereticComponent>(player, out var heretic));
            Assert.That(heretic!.KnowledgeRitualTags, Has.Count.EqualTo(4), "MapInit еретика не отработал");
            Assert.That(SEntMan.GetComponent<MindComponent>(mind).Objectives, Has.Count.EqualTo(3));
        });
    }
}
```

Run: `dotnet build -c Debug` → ожидается ошибка компиляции нет, но тест падает: прототипа `STHeretic` нет.
Run: `dotnet test ... --filter "FullyQualifiedName~HereticRuleTest"` → FAIL (`Unknown prototype STHeretic`).

- [ ] **Step 2: Серверные компоненты и системы**

`Content.Server/_Stories/Heretic/StoriesHereticRuleComponent.cs`:
```csharp
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Stories.Heretic;

[RegisterComponent, Access(typeof(StoriesHereticRuleSystem))]
public sealed partial class StoriesHereticRuleComponent : Component
{
    [DataField]
    public int BaseInfluences = 8;

    [DataField]
    public int InfluencesPerHeretic = 2;

    [DataField]
    public EntProtoId InfluencePrototype = "STHereticInfluence";

    [DataField]
    public bool InfluencesSpawned;

    /// <summary>
    /// Готовая карта Теневого мира от маппера. Если не задана — генерируется в коде.
    /// </summary>
    [DataField]
    public ResPath? ShadowRealmMap;
}
```

`Content.Server/_Stories/Heretic/StoriesHereticRuleSystem.cs`:
```csharp
using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Robust.Shared.Map;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticRuleSystem : GameRuleSystem<StoriesHereticRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private TurfSystem _turf = default!;

    private const int TileAttempts = 10;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<StoriesHereticRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var count = ent.Comp.InfluencesPerHeretic;
        if (!ent.Comp.InfluencesSpawned)
        {
            count += ent.Comp.BaseInfluences;
            ent.Comp.InfluencesSpawned = true;
        }

        for (var i = 0; i < count; i++)
        {
            if (TryGetRandomStationCoordinates(out var coords))
                Spawn(ent.Comp.InfluencePrototype, coords);
        }
    }

    /// <summary>
    /// Случайная проходимая плитка станции.
    /// </summary>
    public bool TryGetRandomStationCoordinates(out EntityCoordinates coords)
    {
        for (var i = 0; i < TileAttempts; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out coords))
                continue;

            if (_turf.GetTileRef(coords) is not { } tile || _turf.IsSpace(tile))
                continue;

            if (_turf.IsTileBlocked(tile, CollisionGroup.Impassable))
                continue;

            return true;
        }

        coords = EntityCoordinates.Invalid;
        return false;
    }

    protected override void AppendRoundEndText(EntityUid uid,
        StoriesHereticRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var heretics = _antag.GetAntagIdentifiers(uid).ToList();
        args.AddLine(Loc.GetString("stories-heretic-round-end-count", ("count", heretics.Count)));

        foreach (var (mindId, data, name) in heretics)
        {
            if (!TryComp<MindComponent>(mindId, out var mind)
                || mind.OwnedEntity is not { } body
                || !TryComp<StoriesHereticComponent>(body, out var heretic))
            {
                args.AddLine(Loc.GetString("stories-heretic-round-end-lost", ("name", name), ("user", data.UserName)));
                continue;
            }

            var path = heretic.Path is { } p
                ? Loc.GetString($"stories-heretic-path-{p.Id}")
                : Loc.GetString("stories-heretic-path-none");

            args.AddLine(Loc.GetString(heretic.Ascended ? "stories-heretic-round-end-ascended" : "stories-heretic-round-end-entry",
                ("name", name),
                ("user", data.UserName),
                ("path", path),
                ("knowledge", heretic.LearnedCount),
                ("sacrifices", heretic.SacrificeCount)));
        }

        args.AddLine("\n");
    }
}
```

`Content.Server/_Stories/Heretic/StoriesHereticObjectiveConditionComponent.cs`:
```csharp
namespace Content.Server._Stories.Heretic;

[RegisterComponent, Access(typeof(StoriesHereticObjectiveSystem))]
public sealed partial class StoriesHereticObjectiveConditionComponent : Component
{
    [DataField(required: true)]
    public StoriesHereticObjectiveKind Kind;

    [DataField(required: true)]
    public LocId Title;
}

public enum StoriesHereticObjectiveKind : byte
{
    Knowledge,
    Sacrifice,
    HeadSacrifice,
}
```

`Content.Server/_Stories/Heretic/StoriesHereticObjectiveSystem.cs`:
```csharp
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server._Stories.Heretic;

/// <summary>
/// Прогресс целей еретика. Требуемые числа хранятся в <see cref="StoriesHereticComponent"/>, чтобы не дублировать их.
/// </summary>
public sealed partial class StoriesHereticObjectiveSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticObjectiveConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StoriesHereticObjectiveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAfterAssign(Entity<StoriesHereticObjectiveConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!TryGetHeretic(args.Mind, out var heretic))
            return;

        var (_, required) = GetCounts(ent.Comp.Kind, heretic);
        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("count", required)), args.Meta);
    }

    private void OnGetProgress(Entity<StoriesHereticObjectiveConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryGetHeretic(args.Mind, out var heretic))
        {
            args.Progress = 0f;
            return;
        }

        var (current, required) = GetCounts(ent.Comp.Kind, heretic);
        args.Progress = required <= 0 ? 1f : Math.Clamp((float) current / required, 0f, 1f);
    }

    private bool TryGetHeretic(MindComponent mind, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out StoriesHereticComponent? heretic)
    {
        heretic = null;
        return mind.OwnedEntity is { } body && TryComp(body, out heretic);
    }

    private static (int Current, int Required) GetCounts(StoriesHereticObjectiveKind kind, StoriesHereticComponent heretic)
    {
        return kind switch
        {
            StoriesHereticObjectiveKind.Knowledge => (heretic.LearnedCount, heretic.RequiredKnowledge),
            StoriesHereticObjectiveKind.Sacrifice => (heretic.SacrificeCount, heretic.RequiredSacrifices),
            StoriesHereticObjectiveKind.HeadSacrifice => (heretic.HeadSacrificeCount, heretic.RequiredHeadSacrifices),
            _ => (0, 0),
        };
    }
}
```

- [ ] **Step 3: YAML роли, правила, целей**

Шаблон целей повторяет `STBaseSlaughterDemonObjective` из `Resources/Prototypes/_Stories/Objectives/demons.yml` (родитель `BaseObjective`, `issuer` — прототип `objectiveIssuer`).

`Resources/Prototypes/_Stories/Heretic/antag.yml`:
```yaml
- type: antag
  id: STHeretic
  name: stories-roles-antag-heretic-name
  antagonist: true
  setPreference: true
  objective: stories-roles-antag-heretic-objective
  guides: [ STHeretic ]
  requirements:
  - !type:OverallPlaytimeRequirement
    time: 5h

- type: antagSpecifier
  parent: BaseTraitor
  id: STHereticSpecifier
  prefRoles: [ STHeretic ]
  briefing:
    text: stories-heretic-role-greeting
    color: "#58a65c"
    sound: /Audio/Ambience/Antag/ling_alert.ogg
  components:
  - type: StoriesHeretic
  mindRoles:
  - STMindRoleHeretic

- type: entity
  parent: BaseMindRoleAntag
  id: STMindRoleHeretic
  name: Heretic Role
  components:
  - type: MindRole
    antagPrototype: STHeretic
    exclusiveAntag: true
    roleType: SoloAntagonist
```

`Resources/Prototypes/_Stories/Heretic/game_rules.yml`:
```yaml
- type: entity
  parent: BaseGameRule
  id: STHeretic
  components:
  - type: GameRule
    minPlayers: 30
  - type: StoriesHereticRule
  - type: AntagObjectives
    objectives:
    - STHereticObjectiveKnowledge
    - STHereticObjectiveSacrifice
    - STHereticObjectiveHeadSacrifice
  - type: AntagSelection
    agentName: stories-heretic-round-end-agent-name
    antags:
    - !type:LinearAntagCount
      proto: STHereticSpecifier
      playerRatio: 24
      range:
        min: 1
        max: 3

- type: gamePreset
  id: STHeretic
  alias:
  - heretic
  name: stories-heretic-title
  description: stories-heretic-description
  showInVote: true
  rules:
  - STHeretic
  - SubGamemodesRule
  - CalmStationEventScheduler
  - MeteorSwarmScheduler
  - BasicRoundstartVariation
```

`Resources/Prototypes/_Stories/Heretic/objectives.yml`:
```yaml
- type: objectiveIssuer
  id: STHeretic
  name: stories-heretic-objective-issuer

- type: entity
  abstract: true
  parent: BaseObjective
  id: STBaseHereticObjective
  components:
  - type: Objective
    difficulty: 1
    issuer: STHeretic
    icon:
      sprite: Objects/Magic/magicactions.rsi
      state: spell_default

- type: entity
  parent: STBaseHereticObjective
  id: STHereticObjectiveKnowledge
  components:
  - type: StoriesHereticObjectiveCondition
    kind: Knowledge
    title: stories-heretic-objective-knowledge

- type: entity
  parent: STBaseHereticObjective
  id: STHereticObjectiveSacrifice
  components:
  - type: StoriesHereticObjectiveCondition
    kind: Sacrifice
    title: stories-heretic-objective-sacrifice

- type: entity
  parent: STBaseHereticObjective
  id: STHereticObjectiveHeadSacrifice
  components:
  - type: StoriesHereticObjectiveCondition
    kind: HeadSacrifice
    title: stories-heretic-objective-head-sacrifice
```

`Resources/Prototypes/_Stories/secret_weights.yml` — в `weights:` прототипа `STSecret` добавить строку:
```yaml
    STHeretic: 0.05
```

- [ ] **Step 4: Локализация правила**

Добавить в `Resources/Locale/ru-RU/_stories/heretic.ftl`:
```ftl
stories-roles-antag-heretic-name = Еретик
stories-roles-antag-heretic-objective = Постигайте запретные знания Мансуса, приносите жертвы и вознеситесь.
stories-heretic-role-greeting = Вы — Еретик. Завеса реальности истончилась, и вы слышите шёпот Мансуса. Собирайте влияния, приносите жертвы и вознеситесь. Откройте дерево знаний действием «Знания Мансуса».
stories-heretic-round-end-agent-name = еретик
stories-heretic-title = Еретики
stories-heretic-description = На станции есть те, кто слышит зов Мансуса.
stories-heretic-round-end-count = Еретиков было: { $count }.
stories-heretic-round-end-entry = { $name } ({ $user }) — путь: { $path }, знаний: { $knowledge }, жертв: { $sacrifices }.
stories-heretic-round-end-ascended = [color=#58a65c]{ $name } ({ $user }) вознёсся![/color] Путь: { $path }, знаний: { $knowledge }, жертв: { $sacrifices }.
stories-heretic-round-end-lost = { $name } ({ $user }) — тело еретика утрачено.
stories-heretic-path-none = не выбран
stories-heretic-path-STHereticPathAsh = Пепел
stories-heretic-objective-issuer = [color=#58a65c]Мансус[/color]
stories-heretic-objective-knowledge = Изучить { $count } знаний Мансуса.
stories-heretic-objective-sacrifice = Принести в жертву { $count } целей.
stories-heretic-objective-head-sacrifice = Принести в жертву главу отдела ({ $count }).
```

Добавить в `Resources/Locale/en-US/_stories/heretic.ftl`:
```ftl
stories-roles-antag-heretic-name = Heretic
stories-roles-antag-heretic-objective = Learn the forbidden knowledge of the Mansus, perform sacrifices and ascend.
stories-heretic-role-greeting = You are a Heretic. The veil of reality has thinned and you hear the whispers of the Mansus. Gather influences, perform sacrifices and ascend. Open the knowledge tree with the "Mansus knowledge" action.
stories-heretic-round-end-agent-name = heretic
stories-heretic-title = Heretics
stories-heretic-description = Some of the crew hear the call of the Mansus.
stories-heretic-round-end-count = There were { $count } heretics.
stories-heretic-round-end-entry = { $name } ({ $user }) — path: { $path }, knowledge: { $knowledge }, sacrifices: { $sacrifices }.
stories-heretic-round-end-ascended = [color=#58a65c]{ $name } ({ $user }) ascended![/color] Path: { $path }, knowledge: { $knowledge }, sacrifices: { $sacrifices }.
stories-heretic-round-end-lost = { $name } ({ $user }) — the heretic's body was lost.
stories-heretic-path-none = none
stories-heretic-path-STHereticPathAsh = Ash
stories-heretic-objective-issuer = [color=#58a65c]The Mansus[/color]
stories-heretic-objective-knowledge = Learn { $count } Mansus knowledges.
stories-heretic-objective-sacrifice = Sacrifice { $count } targets.
stories-heretic-objective-head-sacrifice = Sacrifice a head of staff ({ $count }).
```

- [ ] **Step 5: Влияние-заглушка, чтобы правило не ссылалось на несуществующий прототип**

Создать `Resources/Prototypes/_Stories/Heretic/entities.yml` с минимальным влиянием (полная версия — Task 5):
```yaml
- type: entity
  id: STHereticInfluence
  name: reality smash
  description: A tear in the fabric of reality.
  categories: [ HideSpawnMenu ]
  components:
  - type: Sprite
    sprite: Structures/Magic/Cult/rune.rsi
    state: cult7
    color: "#9b59d0"
  - type: Clickable
  - type: InteractionOutline
```

- [ ] **Step 6: Сборка, тест, линтер**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test ... --filter "FullyQualifiedName~HereticRuleTest"` → PASS.
Run: `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c Debug --no-build` → без ошибок по файлам `_Stories/Heretic`.

- [ ] **Step 7: Коммит**

```bash
git add Content.Server/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Prototypes/_Stories/secret_weights.yml Resources/Locale Content.IntegrationTests/Tests/_Stories/Heretic
git commit -m "feat(heretic): game rule, antag role, objectives, preset"
```

---

### Task 3: Руны и ритуалы

**Files:**
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticRuneComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/StoriesHereticRitualSystem.cs`
- Create: `Resources/Prototypes/_Stories/Heretic/rituals.yml`
- Modify: `Resources/Prototypes/_Stories/Heretic/actions.yml`, `knowledge.yml`, `entities.yml`
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`
- Test: `Content.IntegrationTests/Tests/_Stories/Heretic/HereticRitualTest.cs`

**Interfaces:**
- Consumes: Task 1 API (`TrackLimitedResult`, `CountLimitedResults`, `RemoveRitual`, `AddPoints`, `LockAscension`, `MarkKnowledgeRitualUsed`), `StoriesHereticRitualEffectEvent`.
- Produces:
  - `StoriesHereticRitualSystem.TryPerform(Entity<StoriesHereticComponent> heretic, EntityUid rune) : bool` — сервер.
  - `StoriesHereticRitualSystem.Matches(StoriesHereticIngredient ingredient, EntityUid uid) : bool`.
  - Прототип `STHereticRune`, действие `STActionHereticDrawRune`, стартовое знание `STHereticKnowledgeRunes`.
  - Стартовые ритуалы `STHereticRitualKnowledge`, `STHereticRitualFeastOfOwls` (выдаются знанием `STHereticKnowledgeRunes`).

- [ ] **Step 1: Падающий тест ритуала**

`Content.IntegrationTests/Tests/_Stories/Heretic/HereticRitualTest.cs`:
```csharp
#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticRitualTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: STTestHereticResult
  components:
  - type: Item

- type: entity
  id: STTestHereticRitualLimited
  components:
  - type: StoriesHereticRitual
    limit: 1
    ingredients:
    - name: stories-heretic-ingredient-knife
      whitelist:
        tags: [ Knife ]
    results:
    - STTestHereticResult
";

    [Test]
    public async Task RecipeConsumesAndRespectsLimit()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var heretic = new Entity<StoriesHereticComponent>(uid, comp);
            comp.Rituals.Clear();
            comp.Rituals.Add("STTestHereticRitualLimited");

            var rune = SSpawnAtPosition("STHereticRune", coords);
            var knife = SSpawnAtPosition("KitchenKnife", coords);

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(SEntMan.Deleted(knife) || SEntMan.IsQueuedForDeletion(knife), Is.True, "Нож не поглощён");
            Assert.That(heretics.CountLimitedResults(heretic, "STTestHereticRitualLimited"), Is.EqualTo(1));

            // Лимит 1: повтор не срабатывает.
            SSpawnAtPosition("KitchenKnife", coords);
            Assert.That(rituals.TryPerform(heretic, rune), Is.False);
        });
    }
}
```

Тест пишет в `comp.Rituals` напрямую, поэтому первой строкой стоит `#pragma warning disable RA0002` (анализатор `[Access]`).

Run: `dotnet test ... --filter "FullyQualifiedName~HereticRitualTest"` → FAIL (нет `StoriesHereticRitualSystem` / `STHereticRune` — ошибка компиляции).

- [ ] **Step 2: Компонент руны и система ритуалов**

`Content.Shared/_Stories/Heretic/Components/StoriesHereticRuneComponent.cs`:
```csharp
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticRuneComponent : Component
{
    /// <summary>
    /// Радиус сбора ингредиентов (руна 3×3).
    /// </summary>
    [DataField]
    public float Radius = 1.5f;
}
```

`Content.Shared/_Stories/Heretic/StoriesHereticRitualSystem.cs`:
```csharp
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticRitualSystem : EntitySystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private static readonly ProtoId<TagPrototype> WriteTag = "Write";
    private static readonly SoundSpecifier RitualSound = new SoundPathSpecifier("/Audio/Magic/staff_animation.ogg");

    private readonly HashSet<EntityUid> _nearby = new();
    private readonly HashSet<EntityUid> _used = new();
    private readonly List<EntityUid> _consumed = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticDrawRuneEvent>(OnDrawRune);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticDrawRuneDoAfterEvent>(OnDrawRuneDoAfter);
        SubscribeLocalEvent<StoriesHereticRuneComponent, InteractHandEvent>(OnRuneInteract);
    }

    private void OnDrawRune(Entity<StoriesHereticComponent> ent, ref StoriesHereticDrawRuneEvent args)
    {
        if (args.Handled)
            return;

        if (_hands.GetActiveItem(ent.Owner) is not { } pen || !_tag.HasTag(pen, WriteTag))
        {
            _popup.PopupClient(Loc.GetString("stories-heretic-rune-need-pen"), ent, ent);
            return;
        }

        if (_turf.GetTileRef(args.Target) is not { } tile || _turf.IsSpace(tile))
        {
            _popup.PopupClient(Loc.GetString("stories-heretic-rune-bad-tile"), ent, ent);
            return;
        }

        _nearby.Clear();
        _lookup.GetEntitiesInRange(args.Target, 2f, _nearby);
        foreach (var uid in _nearby)
        {
            if (!HasComp<StoriesHereticRuneComponent>(uid))
                continue;

            _popup.PopupClient(Loc.GetString("stories-heretic-rune-too-close"), ent, ent);
            return;
        }

        var duration = _heretic.HasOpenCodex(ent)
            ? args.Duration / args.CodexSpeedMultiplier
            : args.Duration;

        var doAfter = new DoAfterArgs(EntityManager,
            ent,
            duration,
            new StoriesHereticDrawRuneDoAfterEvent(GetNetCoordinates(args.Target), args.Rune),
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDrawRuneDoAfter(Entity<StoriesHereticComponent> ent, ref StoriesHereticDrawRuneDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        Spawn(args.Rune, GetCoordinates(args.Coordinates));
    }

    private void OnRuneInteract(Entity<StoriesHereticRuneComponent> rune, ref InteractHandEvent args)
    {
        if (args.Handled || !TryComp<StoriesHereticComponent>(args.User, out var heretic))
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        TryPerform((args.User, heretic), rune);
    }

    /// <summary>
    /// Выполняет первый подходящий известный ритуал на руне. Только сервер.
    /// </summary>
    public bool TryPerform(Entity<StoriesHereticComponent> heretic, EntityUid rune)
    {
        var radius = CompOrNull<StoriesHereticRuneComponent>(rune)?.Radius ?? 1.5f;
        var coords = Transform(rune).Coordinates;

        _nearby.Clear();
        _lookup.GetEntitiesInRange(coords, radius, _nearby, LookupFlags.Uncontained);
        _nearby.Remove(heretic);
        _nearby.Remove(rune);

        string? failReason = null;
        foreach (var ritualId in new List<EntProtoId>(heretic.Comp.Rituals))
        {
            if (!ProtoMan.TryIndex(ritualId, out var proto)
                || !proto.TryComp<StoriesHereticRitualComponent>(out var ritual, _factory))
                continue;

            if (ritual.Limit is { } limit && _heretic.CountLimitedResults(heretic, ritualId) >= limit)
            {
                failReason ??= Loc.GetString("stories-heretic-ritual-limit");
                continue;
            }

            if (!TryMatch(ritual.Ingredients, out var missing))
            {
                failReason ??= missing;
                continue;
            }

            if (!TryRunEffect(heretic, rune, ritualId, ritual.Effect, ref failReason))
                continue;

            foreach (var uid in _consumed)
            {
                QueueDel(uid);
            }

            foreach (var result in ritual.Results)
            {
                var spawned = Spawn(result, coords);
                if (ritual.Limit != null)
                    _heretic.TrackLimitedResult(heretic, ritualId, spawned);
            }

            _audio.PlayPvs(RitualSound, rune);
            _popup.PopupEntity(Loc.GetString("stories-heretic-ritual-success"), rune, heretic);
            return true;
        }

        _popup.PopupEntity(failReason ?? Loc.GetString("stories-heretic-ritual-nothing"), rune, heretic);
        return false;
    }

    private bool TryRunEffect(Entity<StoriesHereticComponent> heretic,
        EntityUid rune,
        EntProtoId ritualId,
        StoriesHereticRitualEffect effect,
        ref string? failReason)
    {
        switch (effect)
        {
            case StoriesHereticRitualEffect.None:
                return true;

            case StoriesHereticRitualEffect.Knowledge:
                if (heretic.Comp.KnowledgeRitualUsed)
                    return false;

                foreach (var tag in heretic.Comp.KnowledgeRitualTags)
                {
                    var found = false;
                    foreach (var uid in _nearby)
                    {
                        if (_used.Contains(uid) || !_tag.HasTag(uid, tag))
                            continue;

                        _used.Add(uid);
                        _consumed.Add(uid);
                        found = true;
                        break;
                    }

                    if (found)
                        continue;

                    failReason ??= Loc.GetString("stories-heretic-ritual-missing",
                        ("item", Loc.GetString($"stories-heretic-tag-{tag.Id}")),
                        ("amount", 1));
                    return false;
                }

                _heretic.MarkKnowledgeRitualUsed(heretic);
                _heretic.RemoveRitual(heretic, ritualId);
                _heretic.AddPoints(heretic, heretic.Comp.KnowledgeRitualPoints);
                return true;

            case StoriesHereticRitualEffect.FeastOfOwls:
                _heretic.LockAscension(heretic);
                _heretic.RemoveRitual(heretic, ritualId);
                _heretic.AddPoints(heretic, heretic.Comp.FeastOfOwlsPoints);
                return true;

            default:
                var ev = new StoriesHereticRitualEffectEvent(heretic, rune, ritualId, effect, _nearby, _consumed);
                RaiseLocalEvent(ref ev);
                if (!ev.Success)
                    failReason ??= ev.FailReason;

                return ev.Success;
        }
    }

    private bool TryMatch(List<StoriesHereticIngredient> ingredients, out string? missing)
    {
        _used.Clear();
        _consumed.Clear();
        missing = null;

        foreach (var ingredient in ingredients)
        {
            var found = 0;
            foreach (var uid in _nearby)
            {
                if (found >= ingredient.Amount)
                    break;

                if (_used.Contains(uid) || !Matches(ingredient, uid))
                    continue;

                _used.Add(uid);
                found++;
                if (ingredient.Consume)
                    _consumed.Add(uid);
            }

            if (found >= ingredient.Amount)
                continue;

            missing = Loc.GetString("stories-heretic-ritual-missing",
                ("item", Loc.GetString(ingredient.Name)),
                ("amount", ingredient.Amount - found));
            return false;
        }

        return true;
    }

    public bool Matches(StoriesHereticIngredient ingredient, EntityUid uid)
    {
        if (ingredient.Whitelist != null && _whitelist.IsValid(ingredient.Whitelist, uid))
            return true;

        if (ingredient.Prototypes != null
            && MetaData(uid).EntityPrototype is { } proto
            && ingredient.Prototypes.Contains(proto.ID))
            return true;

        if (ingredient.OrganCategory != null
            && TryComp<OrganComponent>(uid, out var organ)
            && organ.Category == ingredient.OrganCategory)
            return true;

        return false;
    }
}
```

`HasOpenCodex` появится в Task 4. Чтобы собрать проект сейчас, временно добавьте в `StoriesHereticSystem.Focus.cs`:
```csharp
    public bool HasOpenCodex(EntityUid uid)
    {
        return false;
    }
```
(Task 4 заменит это реальной реализацией.)

- [ ] **Step 3: YAML руны, действия, стартового знания и ритуалов**

Добавить в `Resources/Prototypes/_Stories/Heretic/entities.yml`:
```yaml
- type: entity
  id: STHereticRune
  name: transmutation rune
  description: A carefully drawn circle of eldritch symbols.
  categories: [ HideSpawnMenu ]
  placement:
    mode: SnapgridCenter
  components:
  - type: StoriesHereticRune
  - type: Sprite
    sprite: Structures/Magic/Cult/rune.rsi
    state: cult3
    color: "#4bd36a"
    scale: 1.5, 1.5
    drawdepth: FloorObjects
  - type: Clickable
  - type: InteractionOutline
  - type: Physics
    bodyType: Static
    canCollide: false
  - type: Transform
    anchored: true
```

Добавить в `Resources/Prototypes/_Stories/Heretic/actions.yml`:
```yaml
- type: entity
  id: STActionHereticDrawRune
  name: Draw transmutation rune
  description: Use a writing tool in your active hand to draw a transmutation rune.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    useDelay: 1
  - type: Sprite
    sprite: Structures/Magic/Cult/rune.rsi
    layers:
    - state: cult3
  - type: TargetAction
    range: 1.5
  - type: WorldTargetAction
    event: !type:StoriesHereticDrawRuneEvent
```

Добавить в `Resources/Prototypes/_Stories/Heretic/knowledge.yml`:
```yaml
- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeRunes
  name: Mansus transmutation
  description: You know how to draw transmutation runes and perform simple rituals.
  components:
  - type: StoriesHereticKnowledge
    starting: true
    cost: 0
    actions:
    - STActionHereticDrawRune
    rituals:
    - STHereticRitualKnowledge
    - STHereticRitualFeastOfOwls
```

`Resources/Prototypes/_Stories/Heretic/rituals.yml`:
```yaml
- type: entity
  abstract: true
  id: STHereticRitualBase
  categories: [ HideSpawnMenu ]
  components:
  - type: StoriesHereticRitual

- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualKnowledge
  name: Ritual of Knowledge
  description: Gather the four items the Mansus demands to gain knowledge. Can be performed once.
  components:
  - type: StoriesHereticRitual
    effect: Knowledge

- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualFeastOfOwls
  name: Feast of Owls
  description: Gain great knowledge at once, but forever lose the chance to ascend.
  components:
  - type: StoriesHereticRitual
    effect: FeastOfOwls
```

- [ ] **Step 4: Локализация рун и ритуалов**

Добавить в ru-RU `heretic.ftl`:
```ftl
stories-heretic-rune-need-pen = Нужна ручка или другой пишущий предмет в активной руке.
stories-heretic-rune-bad-tile = Здесь нельзя начертить руну.
stories-heretic-rune-too-close = Слишком близко к другой руне.
stories-heretic-ritual-success = Руна вспыхивает, и ритуал свершается.
stories-heretic-ritual-nothing = Руна безмолвствует: ни один ритуал не подходит.
stories-heretic-ritual-limit = Мансус не даст вам больше таких предметов.
stories-heretic-ritual-missing = Не хватает: { $item } ×{ $amount }.
stories-heretic-tag-Knife = нож
stories-heretic-tag-Candle = свеча
stories-heretic-tag-Book = книга
stories-heretic-tag-GlassShard = осколок стекла
stories-heretic-tag-Matchstick = спичка
stories-heretic-tag-Write = пишущий предмет
stories-heretic-tag-Sheet = лист материала
stories-heretic-ingredient-knife = нож
```

Добавить в en-US `heretic.ftl`:
```ftl
stories-heretic-rune-need-pen = You need a pen or another writing tool in your active hand.
stories-heretic-rune-bad-tile = You cannot draw a rune here.
stories-heretic-rune-too-close = Too close to another rune.
stories-heretic-ritual-success = The rune flares and the ritual is complete.
stories-heretic-ritual-nothing = The rune stays silent: no ritual fits.
stories-heretic-ritual-limit = The Mansus will not grant you more of these.
stories-heretic-ritual-missing = Missing: { $item } ×{ $amount }.
stories-heretic-tag-Knife = knife
stories-heretic-tag-Candle = candle
stories-heretic-tag-Book = book
stories-heretic-tag-GlassShard = glass shard
stories-heretic-tag-Matchstick = matchstick
stories-heretic-tag-Write = writing tool
stories-heretic-tag-Sheet = material sheet
stories-heretic-ingredient-knife = knife
```

- [ ] **Step 5: Сборка и тесты**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test ... --filter "FullyQualifiedName~Heretic"` → PASS (Knowledge, Rule, Ritual).

- [ ] **Step 6: Коммит**

```bash
git add Content.Shared/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Locale Content.IntegrationTests/Tests/_Stories/Heretic
git commit -m "feat(heretic): transmutation runes and ritual matching"
```

---

### Task 4: Фокус, Кодекс, Янтарный фокус

**Files:**
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticFocusComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticCodexComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticRequiresFocusComponent.cs`
- Modify: `Content.Shared/_Stories/Heretic/StoriesHereticSystem.Focus.cs` (заменить заглушки целиком)
- Modify: `Resources/Prototypes/_Stories/Heretic/entities.yml`, `rituals.yml`, `knowledge.yml`
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`
- Test: `Content.IntegrationTests/Tests/_Stories/Heretic/HereticFocusTest.cs`

**Interfaces:**
- Produces:
  - `StoriesHereticSystem.HasFocus(EntityUid uid) : bool`
  - `StoriesHereticSystem.HasOpenCodex(EntityUid uid) : bool`
  - `StoriesHereticCodexComponent.InfluenceMultiplier : int` (по умолчанию 2) — читает Task 7.
  - `StoriesHereticRequiresFocusComponent` — вешается на прототипы действий в Task 9.
  - Прототипы `STHereticAmberFocus`, `STHereticCodexCicatrix`.

- [ ] **Step 1: Падающий тест фокуса**

`Content.IntegrationTests/Tests/_Stories/Heretic/HereticFocusTest.cs`:
```csharp
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Inventory;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticFocusTest : GameTest
{
    [Test]
    public async Task AmberFocusGivesFocus()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var inventory = Server.System<InventorySystem>();

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawnAtPosition("MobHuman", testMap.GridCoords);
            SEntMan.AddComponent<StoriesHereticComponent>(uid);
            Assert.That(heretics.HasFocus(uid), Is.False);

            var focus = SSpawnAtPosition("STHereticAmberFocus", testMap.GridCoords);
            Assert.That(inventory.TryEquip(uid, focus, "neck", silent: true, force: true), Is.True);
            Assert.That(heretics.HasFocus(uid), Is.True);
        });
    }
}
```

Run: `dotnet build -c Debug` → ошибка компиляции (`HasFocus` не существует). Это ожидаемый «красный» шаг.

- [ ] **Step 2: Компоненты**

`Content.Shared/_Stories/Heretic/Components/StoriesHereticFocusComponent.cs`:
```csharp
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Надетый предмет служит фокусом для сильных заклинаний еретика.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticFocusComponent : Component;
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticCodexComponent.cs`:
```csharp
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StoriesHereticSystem))]
public sealed partial class StoriesHereticCodexComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Open;

    [DataField]
    public int InfluenceMultiplier = 2;
}
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticRequiresFocusComponent.cs`:
```csharp
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Вешается на сущность действия: действие не срабатывает без фокуса.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticRequiresFocusComponent : Component;
```

- [ ] **Step 3: Логика фокуса**

`Content.Shared/_Stories/Heretic/StoriesHereticSystem.Focus.cs` (полностью заменить файл):
```csharp
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticSystem
{
    private const SlotFlags FocusSlots = SlotFlags.NECK | SlotFlags.EYES | SlotFlags.HEAD | SlotFlags.OUTERCLOTHING;

    private void InitializeFocus()
    {
        SubscribeLocalEvent<StoriesHereticRequiresFocusComponent, ActionAttemptEvent>(OnRequiresFocusAttempt);
        SubscribeLocalEvent<StoriesHereticCodexComponent, UseInHandEvent>(OnCodexUse);
    }

    private void OnRequiresFocusAttempt(Entity<StoriesHereticRequiresFocusComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled || HasFocus(args.User))
            return;

        args.Cancelled = true;
        args.Reason = Loc.GetString("stories-heretic-no-focus");
    }

    private void OnCodexUse(Entity<StoriesHereticCodexComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !HasComp<StoriesHereticComponent>(args.User))
            return;

        args.Handled = true;
        ent.Comp.Open = !ent.Comp.Open;
        Dirty(ent);

        _popup.PopupClient(Loc.GetString(ent.Comp.Open ? "stories-heretic-codex-open" : "stories-heretic-codex-close"),
            args.User,
            args.User);
    }

    public bool HasFocus(EntityUid uid)
    {
        if (TryComp<StoriesHereticComponent>(uid, out var heretic) && heretic.Ascended)
            return true;

        if (HasOpenCodex(uid))
            return true;

        var slots = _inventory.GetSlotEnumerator(uid, FocusSlots);
        while (slots.NextItem(out var item))
        {
            if (HasComp<StoriesHereticFocusComponent>(item))
                return true;
        }

        return false;
    }

    public bool HasOpenCodex(EntityUid uid)
    {
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            if (TryComp<StoriesHereticCodexComponent>(held, out var codex) && codex.Open)
                return true;
        }

        return false;
    }
}
```

- [ ] **Step 4: YAML предметов, ритуалов, стартовых знаний**

Добавить в `entities.yml`:
```yaml
- type: entity
  parent: ClothingNeckBase
  id: STHereticAmberFocus
  name: amber focus
  description: A glass amulet with an eye frozen inside. It helps to focus on the Mansus.
  components:
  - type: StoriesHereticFocus
  - type: Sprite
    sprite: Clothing/Neck/Misc/bling.rsi
    color: "#e0a030"
  - type: Clothing
    sprite: Clothing/Neck/Misc/bling.rsi

- type: entity
  parent: BaseItem
  id: STHereticCodexCicatrix
  name: Codex Cicatrix
  description: A book bound in skin. Its pages whisper.
  components:
  - type: StoriesHereticCodex
  - type: Sprite
    sprite: Objects/Misc/books.rsi
    state: book_icon
    color: "#7a3b3b"
  - type: Item
    size: Normal
```

Добавить в `rituals.yml`:
```yaml
- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualAmberFocus
  name: Amber Focus
  description: A glass sheet and a pair of eyes.
  components:
  - type: StoriesHereticRitual
    ingredients:
    - name: stories-heretic-ingredient-glass-sheet
      prototypes: [ SheetGlass ]
    - name: stories-heretic-ingredient-eyes
      organCategory: Eyes
    results:
    - STHereticAmberFocus

- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualCodex
  name: Codex Cicatrix
  description: A book, a writing tool and hide or leather.
  components:
  - type: StoriesHereticRitual
    ingredients:
    - name: stories-heretic-ingredient-book
      whitelist:
        tags: [ Book ]
    - name: stories-heretic-ingredient-pen
      whitelist:
        tags: [ Write ]
    - name: stories-heretic-ingredient-hide
      prototypes: [ MaterialHideBear, MaterialHideCorgi, ClothingHandsGlovesLeather ]
    results:
    - STHereticCodexCicatrix
```

Добавить в `knowledge.yml`:
```yaml
- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAmberFocus
  name: Amber Focus
  description: You know how to create a focus from glass and eyes.
  components:
  - type: StoriesHereticKnowledge
    starting: true
    cost: 0
    rituals:
    - STHereticRitualAmberFocus

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeCodex
  name: Codex Cicatrix
  description: You know how to bind the Codex. An open Codex doubles influences, speeds up runes and works as a focus.
  components:
  - type: StoriesHereticKnowledge
    starting: true
    cost: 0
    rituals:
    - STHereticRitualCodex
```

- [ ] **Step 5: Локализация**

ru-RU:
```ftl
stories-heretic-no-focus = Без фокуса это заклинание не подвластно вам.
stories-heretic-codex-open = Вы раскрываете Кодекс. Страницы шепчут.
stories-heretic-codex-close = Вы закрываете Кодекс.
stories-heretic-ingredient-glass-sheet = лист стекла
stories-heretic-ingredient-eyes = глаза
stories-heretic-ingredient-book = книга
stories-heretic-ingredient-pen = пишущий предмет
stories-heretic-ingredient-hide = шкура или кожа
```
en-US:
```ftl
stories-heretic-no-focus = You cannot cast this without a focus.
stories-heretic-codex-open = You open the Codex. Its pages whisper.
stories-heretic-codex-close = You close the Codex.
stories-heretic-ingredient-glass-sheet = glass sheet
stories-heretic-ingredient-eyes = eyes
stories-heretic-ingredient-book = book
stories-heretic-ingredient-pen = writing tool
stories-heretic-ingredient-hide = hide or leather
```

- [ ] **Step 6: Сборка, тесты, коммит**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test ... --filter "FullyQualifiedName~Heretic"` → PASS.
```bash
git add Content.Shared/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Locale Content.IntegrationTests/Tests/_Stories/Heretic
git commit -m "feat(heretic): focus, Codex Cicatrix, Amber Focus"
```

---

### Task 5: Хватка Мансуса, метки, клинок, ответ жертв

**Files:**
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticBladeComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticMarkComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticGraspMarkComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticGraspBlindComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticSacrificedComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/StoriesHereticGraspSystem.cs`
- Modify: `Resources/Prototypes/_Stories/Heretic/actions.yml`, `knowledge.yml`
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`

**Interfaces:**
- Consumes: `StoriesHereticSystem.HasFocus` (Task 4), события `StoriesHereticGraspHitEvent`, `StoriesHereticMarkTriggeredEvent`, `StoriesHereticBladeHitEvent` (Task 1).
- Produces:
  - Действие `STActionHereticGrasp` и стартовое знание `STHereticKnowledgeBreakOfDawn`.
  - `StoriesHereticBladeComponent` (вешается на клинки путей в Task 9).
  - `StoriesHereticGraspMarkComponent { Path, Duration }`, `StoriesHereticGraspBlindComponent { Duration }` — добавляются знаниями пути через `addComponents`.
  - `StoriesHereticMarkComponent { Path, EndTime, Hops }`.
  - `StoriesHereticSacrificedComponent` — вешает Task 6.

- [ ] **Step 1: Компоненты**

`Content.Shared/_Stories/Heretic/Components/StoriesHereticBladeComponent.cs`:
```csharp
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticBladeComponent : Component
{
    [DataField]
    public float TeleportMin = 10f;

    [DataField]
    public float TeleportMax = 20f;

    [DataField]
    public TimeSpan BreakTime = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier? BreakSound = new SoundPathSpecifier("/Audio/Effects/glass_break1.ogg");
}
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticMarkComponent.cs`:
```csharp
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Метка пути на цели. Срабатывает от удара клинком того же пути.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class StoriesHereticMarkComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype> Path;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Сколько раз метка уже перескакивала (для меток, которые распространяются).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Hops;
}
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticGraspMarkComponent.cs`:
```csharp
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// На еретике: Хватка ставит метку пути.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticGraspMarkComponent : Component
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Path;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);
}
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticGraspBlindComponent.cs`:
```csharp
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// На еретике: Хватка ослепляет.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticGraspBlindComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);
}
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticSacrificedComponent.cs`:
```csharp
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Бывшая жертва: не может навредить еретику с фокусом.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticSacrificedComponent : Component
{
    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(3);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new() { { "Blunt", 15 } },
    };
}
```

- [ ] **Step 2: Система**

`Content.Shared/_Stories/Heretic/StoriesHereticGraspSystem.cs`:
```csharp
using System.Linq;
using System.Numerics;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared._Stories.Holy;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticGraspSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private SharedHolySystem _holy = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private static readonly EntProtoId SlurredEffect = "StatusEffectSlurred";
    private static readonly EntProtoId BlindnessEffect = "StatusEffectBlindness";
    private const int TeleportAttempts = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticGraspEvent>(OnGrasp);
        SubscribeLocalEvent<StoriesHereticGraspBlindComponent, StoriesHereticGraspHitEvent>(OnGraspBlind);
        SubscribeLocalEvent<StoriesHereticGraspMarkComponent, StoriesHereticGraspHitEvent>(OnGraspMark);
        SubscribeLocalEvent<StoriesHereticBladeComponent, MeleeHitEvent>(OnBladeHit);
        SubscribeLocalEvent<StoriesHereticBladeComponent, UseInHandEvent>(OnBladeUse);
        SubscribeLocalEvent<StoriesHereticBladeComponent, StoriesHereticBladeBreakDoAfterEvent>(OnBladeBreak);
        SubscribeLocalEvent<StoriesHereticSacrificedComponent, AttackedEvent>(OnSacrificedAttacked);
        SubscribeLocalEvent<StoriesHereticComponent, AttackedEvent>(OnHereticAttacked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticMarkComponent>();
        while (query.MoveNext(out var uid, out var mark))
        {
            if (now >= mark.EndTime)
                RemCompDeferred<StoriesHereticMarkComponent>(uid);
        }
    }

    private void OnGrasp(Entity<StoriesHereticComponent> ent, ref StoriesHereticGraspEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (HasComp<StoriesHereticRuneComponent>(target))
        {
            args.Handled = true;
            if (_net.IsServer)
                QueueDel(target);

            return;
        }

        if (target == ent.Owner || !HasComp<MobStateComponent>(target))
            return;

        args.Handled = true;

        if (IsProtected(target))
        {
            _popup.PopupPredicted(Loc.GetString("stories-heretic-grasp-blocked"), target, ent);
            return;
        }

        _stamina.TakeStaminaDamage(target, args.StaminaDamage, source: ent);
        _stun.TryKnockdown(target, args.KnockdownTime);
        _statusEffects.TryUpdateStatusEffectDuration(target, SlurredEffect, args.SlurTime);

        var ev = new StoriesHereticGraspHitEvent(ent, target);
        RaiseLocalEvent(ent, ref ev);
    }

    private bool IsProtected(EntityUid target)
    {
        return _holy.IsHoly(target) || _hands.EnumerateHeld(target).Any(HasComp<HolyComponent>);
    }

    private void OnGraspBlind(Entity<StoriesHereticGraspBlindComponent> ent, ref StoriesHereticGraspHitEvent args)
    {
        _statusEffects.TryUpdateStatusEffectDuration(args.Target, BlindnessEffect, ent.Comp.Duration);
    }

    private void OnGraspMark(Entity<StoriesHereticGraspMarkComponent> ent, ref StoriesHereticGraspHitEvent args)
    {
        ApplyMark(args.Target, ent.Comp.Path, ent.Comp.Duration, 0);
    }

    public void ApplyMark(EntityUid target, ProtoId<TagPrototype> path, TimeSpan duration, int hops)
    {
        var mark = EnsureComp<StoriesHereticMarkComponent>(target);
        mark.Path = path;
        mark.EndTime = _timing.CurTime + duration;
        mark.Hops = hops;
        Dirty(target, mark);
    }

    private void OnBladeHit(Entity<StoriesHereticBladeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !TryComp<StoriesHereticComponent>(args.User, out var heretic))
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == args.User)
                continue;

            var hit = new StoriesHereticBladeHitEvent(args.User, target, ent);
            RaiseLocalEvent(args.User, ref hit);

            if (!TryComp<StoriesHereticMarkComponent>(target, out var mark) || mark.Path != heretic.Path)
                continue;

            var triggered = new StoriesHereticMarkTriggeredEvent(args.User, target, mark.Path, mark.Hops);
            RemComp(target, mark);
            RaiseLocalEvent(ref triggered);
        }
    }

    private void OnBladeUse(Entity<StoriesHereticBladeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !HasComp<StoriesHereticComponent>(args.User))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.BreakTime,
            new StoriesHereticBladeBreakDoAfterEvent(),
            ent,
            used: ent)
        {
            BreakOnMove = false,
            NeedHand = true,
        });
    }

    private void OnBladeBreak(Entity<StoriesHereticBladeComponent> ent, ref StoriesHereticBladeBreakDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        var user = args.User;
        var origin = _transform.GetMapCoordinates(user);
        for (var i = 0; i < TeleportAttempts; i++)
        {
            var distance = _random.NextFloat(ent.Comp.TeleportMin, ent.Comp.TeleportMax);
            var offset = _random.NextAngle().ToVec() * distance;
            var coords = _transform.ToCoordinates(new MapCoordinates(origin.Position + offset, origin.MapId));

            if (_turf.GetTileRef(coords) is not { } tile
                || _turf.IsSpace(tile)
                || _turf.IsTileBlocked(tile, CollisionGroup.Impassable))
                continue;

            _audio.PlayPvs(ent.Comp.BreakSound, user);
            _transform.SetCoordinates(user, coords);
            _transform.AttachToGridOrMap(user);
            QueueDel(ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString("stories-heretic-blade-no-place"), user, user);
    }

    private void OnSacrificedAttacked(Entity<StoriesHereticSacrificedComponent> ent, ref AttackedEvent args)
    {
        if (HasComp<StoriesHereticComponent>(args.User) && _heretic.HasFocus(args.User))
            Punish(ent);
    }

    private void OnHereticAttacked(Entity<StoriesHereticComponent> ent, ref AttackedEvent args)
    {
        if (TryComp<StoriesHereticSacrificedComponent>(args.User, out var sacrificed) && _heretic.HasFocus(ent))
            Punish((args.User, sacrificed));
    }

    private void Punish(Entity<StoriesHereticSacrificedComponent> ent)
    {
        _stun.TryKnockdown(ent.Owner, ent.Comp.KnockdownTime);
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, true);
    }
}
```

`HolyComponent` лежит в `Content.Shared._Stories.Holy` (тот же `using`, что и `SharedHolySystem`). Публичный `ApplyMark` нужен Task 9 (распространение метки Пепла).

- [ ] **Step 3: YAML действия и стартового знания**

Добавить в `actions.yml`:
```yaml
- type: entity
  id: STActionHereticGrasp
  name: Mansus Grasp
  description: Touch a creature to stagger it with the power of the Mansus. Also erases transmutation runes.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    useDelay: 10
    sound: !type:SoundPathSpecifier
      path: /Audio/Magic/disintegrate.ogg
  - type: Sprite
    sprite: Objects/Magic/magicactions.rsi
    layers:
    - state: blink
  - type: TargetAction
    range: 1.5
    interactOnMiss: false
  - type: EntityTargetAction
    event: !type:StoriesHereticGraspEvent
    canTargetSelf: false
    whitelist:
      components:
      - MobState
      - StoriesHereticRune
```

Добавить в `knowledge.yml`:
```yaml
- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeBreakOfDawn
  name: Break of Dawn
  description: The Mansus Grasp staggers, knocks down and confuses its victim.
  components:
  - type: StoriesHereticKnowledge
    starting: true
    cost: 0
    actions:
    - STActionHereticGrasp
```

- [ ] **Step 4: Локализация**

ru-RU:
```ftl
stories-heretic-grasp-blocked = Святая сила отталкивает Хватку!
stories-heretic-blade-no-place = Мансусу некуда вас перенести.
```
en-US:
```ftl
stories-heretic-grasp-blocked = Holy power repels the Grasp!
stories-heretic-blade-no-place = The Mansus has nowhere to take you.
```

- [ ] **Step 5: Сборка, тесты, коммит**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test ... --filter "FullyQualifiedName~Heretic"` → PASS.
Run: YAML-линтер → без ошибок.
```bash
git add Content.Shared/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Locale
git commit -m "feat(heretic): Mansus Grasp, path marks, heretic blade"
```

---

### Task 6: Жертвоприношение, Теневой мир, Живое сердце

**Files:**
- Create: `Content.Server/_Stories/Heretic/StoriesHereticShadowRealmComponent.cs`
- Create: `Content.Server/_Stories/Heretic/StoriesHereticSacrificeSystem.cs`
- Create: `Content.Server/_Stories/Heretic/StoriesHereticSacrificeSystem.ShadowRealm.cs`
- Modify: `Content.Server/_Stories/Heretic/StoriesHereticRuleSystem.cs` (вызов `AssignTargets`)
- Modify: `Resources/Prototypes/_Stories/Heretic/actions.yml`, `knowledge.yml`, `rituals.yml`
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`
- Test: `Content.IntegrationTests/Tests/_Stories/Heretic/HereticSacrificeTest.cs`

**Interfaces:**
- Consumes: `StoriesHereticSystem.SetTargets`, `RegisterSacrifice`, `CycleTrackedTarget` (Task 1), `StoriesHereticRitualEffectEvent` (Task 1/3), `StoriesHereticRuleSystem.TryGetRandomStationCoordinates` (Task 2), `StoriesHereticSacrificedComponent` (Task 5).
- Produces:
  - `StoriesHereticSacrificeSystem.AssignTargets(Entity<StoriesHereticComponent> heretic)`
  - `StoriesHereticSacrificeSystem.IsHead(EntityUid uid) : bool`
  - Ритуалы `STHereticRitualSacrifice`, `STHereticRitualReroll`; действие `STActionHereticLivingHeart`.

- [ ] **Step 1: Падающий тест жертвоприношения**

`Content.IntegrationTests/Tests/_Stories/Heretic/HereticSacrificeTest.cs`:
```csharp
#pragma warning disable RA0002
using Content.IntegrationTests.Fixtures;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Stories.Heretic;

[TestFixture]
public sealed class HereticSacrificeTest : GameTest
{
    [Test]
    public async Task SacrificeTarget()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();
        var damageable = Server.System<DamageableSystem>();
        var mobState = Server.System<MobStateSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var heretic = new Entity<StoriesHereticComponent>(uid, comp);
            var points = comp.Points;

            var rune = SSpawnAtPosition("STHereticRune", coords);
            var stranger = SSpawnAtPosition("MobHuman", coords);
            damageable.TryChangeDamage(stranger, new DamageSpecifier { DamageDict = new() { { "Blunt", 300 } } }, true);
            Assert.That(mobState.IsDead(stranger));

            // Не цель — не принимается.
            heretics.SetTargets(heretic, new());
            Assert.That(rituals.TryPerform(heretic, rune), Is.False);

            // Цель — принимается.
            heretics.SetTargets(heretic, new() { stranger });
            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(comp.SacrificeCount, Is.EqualTo(1));
            Assert.That(comp.Points, Is.EqualTo(points + comp.SacrificePoints));
            Assert.That(SEntMan.HasComponent<StoriesHereticSacrificedComponent>(stranger));
            Assert.That(mobState.IsAlive(stranger), "Жертва не исцелена");
            Assert.That(SEntMan.GetComponent<TransformComponent>(stranger).MapID, Is.Not.EqualTo(testMap.MapId),
                "Жертва не перенесена в Теневой мир");
        });
    }
}
```

Run: `dotnet build -c Debug && dotnet test ... --filter "FullyQualifiedName~HereticSacrificeTest"` → FAIL (ритуал жертвы не известен).

- [ ] **Step 2: Компонент Теневого мира**

`Content.Server/_Stories/Heretic/StoriesHereticShadowRealmComponent.cs`:
```csharp
using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticSacrificeSystem))]
public sealed partial class StoriesHereticShadowRealmComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ReturnTime;

    [DataField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDamage;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new() { { "Cold", 4 } },
    };

    [DataField]
    public TimeSpan HallucinationTime = TimeSpan.FromSeconds(6);
}
```

- [ ] **Step 3: Система жертвоприношения**

`Content.Server/_Stories/Heretic/StoriesHereticSacrificeSystem.cs`:
```csharp
using Content.Server.Popups;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Administration.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cuffs;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticSacrificeSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private StoriesHereticRuleSystem _rule = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<DepartmentPrototype> CommandDepartment = "Command";
    private static readonly ProtoId<DepartmentPrototype> SecurityDepartment = "Security";
    private static readonly EntProtoId HallucinationEffect = "StatusEffectSeeingRainbow";
    private static readonly EntProtoId MessPrototype = "PuddleBlood";

    private readonly List<(EntityUid Uid, ProtoId<JobPrototype>? Job)> _candidates = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticLivingHeartEvent>(OnLivingHeart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoriesHereticShadowRealmComponent>();
        while (query.MoveNext(out var uid, out var realm))
        {
            if (now >= realm.ReturnTime)
            {
                ReturnFromRealm((uid, realm));
                continue;
            }

            if (now < realm.NextDamage)
                continue;

            realm.NextDamage = now + realm.DamageInterval;
            _damageable.TryChangeDamage(uid, realm.Damage, true);
            _statusEffects.TryUpdateStatusEffectDuration(uid, HallucinationEffect, realm.HallucinationTime);
        }
    }

    #region Targets

    public void AssignTargets(Entity<StoriesHereticComponent> heretic)
    {
        _candidates.Clear();
        var query = EntityQueryEnumerator<HumanoidProfileComponent, MobStateComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState, out _))
        {
            if (uid == heretic.Owner
                || HasComp<StoriesHereticComponent>(uid)
                || !_mobState.IsAlive(uid, mobState)
                || !_mind.TryGetMind(uid, out var mindId, out _))
                continue;

            _jobs.MindTryGetJobId(mindId, out var job);
            _candidates.Add((uid, job));
        }

        _random.Shuffle(_candidates);

        var picked = new List<EntityUid>();
        TryPick(picked, job => InDepartment(job, CommandDepartment));
        TryPick(picked, job => InDepartment(job, SecurityDepartment));
        if (GetPrimaryDepartment(heretic) is { } own)
            TryPick(picked, job => InDepartment(job, own));

        while (picked.Count < heretic.Comp.TargetCount && TryPick(picked, _ => true))
        {
        }

        _heretic.SetTargets(heretic, picked);
    }

    private bool TryPick(List<EntityUid> picked, Func<ProtoId<JobPrototype>?, bool> predicate)
    {
        for (var i = 0; i < _candidates.Count; i++)
        {
            if (!predicate(_candidates[i].Job))
                continue;

            picked.Add(_candidates[i].Uid);
            _candidates.RemoveAt(i);
            return true;
        }

        return false;
    }

    private bool InDepartment(ProtoId<JobPrototype>? job, ProtoId<DepartmentPrototype> department)
    {
        return job is { } jobId && ProtoMan.Index(department).Roles.Contains(jobId);
    }

    private ProtoId<DepartmentPrototype>? GetPrimaryDepartment(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !_jobs.MindTryGetJobId(mindId, out var job)
            || job is not { } jobId
            || !_jobs.TryGetPrimaryDepartment(jobId.Id, out var department))
            return null;

        return department.ID;
    }

    public bool IsHead(EntityUid uid)
    {
        return _mind.TryGetMind(uid, out var mindId, out _)
               && _jobs.MindTryGetJobId(mindId, out var job)
               && InDepartment(job, CommandDepartment);
    }

    #endregion

    #region Rituals

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        switch (args.Effect)
        {
            case StoriesHereticRitualEffect.Sacrifice:
                TrySacrifice(ref args);
                break;
            case StoriesHereticRitualEffect.RerollTargets:
                AssignTargets(args.Heretic);
                args.Success = true;
                break;
        }
    }

    private void TrySacrifice(ref StoriesHereticRitualEffectEvent args)
    {
        var heretic = args.Heretic;
        foreach (var uid in args.Nearby)
        {
            if (!HasComp<HumanoidProfileComponent>(uid) || !HasComp<MobStateComponent>(uid))
                continue;

            if (!heretic.Comp.Targets.Contains(GetNetEntity(uid)))
            {
                args.FailReason ??= Loc.GetString("stories-heretic-sacrifice-not-target");
                continue;
            }

            if (!CanBeSacrificed(uid))
            {
                args.FailReason ??= Loc.GetString("stories-heretic-sacrifice-resisting");
                continue;
            }

            Sacrifice(heretic, uid, Transform(args.Rune).Coordinates);
            args.Success = true;
            return;
        }

        args.FailReason ??= Loc.GetString("stories-heretic-sacrifice-no-body");
    }

    private bool CanBeSacrificed(EntityUid uid)
    {
        return _mobState.IsIncapacitated(uid)
               || _cuffable.IsCuffed(uid)
               || HasComp<SleepingComponent>(uid);
    }

    private void Sacrifice(Entity<StoriesHereticComponent> heretic, EntityUid victim, EntityCoordinates runeCoords)
    {
        _heretic.RegisterSacrifice(heretic, victim, IsHead(victim));

        if (!_mind.TryGetMind(victim, out _, out _))
            Spawn(MessPrototype, runeCoords);

        _rejuvenate.PerformRejuvenate(victim);
        EnsureComp<StoriesHereticSacrificedComponent>(victim);
        SendToShadowRealm(victim);
        _popup.PopupEntity(Loc.GetString("stories-heretic-sacrifice-victim"), victim, victim, PopupType.LargeCaution);
    }

    #endregion

    #region Living heart

    private void OnLivingHeart(Entity<StoriesHereticComponent> ent, ref StoriesHereticLivingHeartEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _audio.PlayPvs(args.Sound, ent);

        if (_heretic.CycleTrackedTarget(ent) is not { } target)
        {
            _popup.PopupEntity(Loc.GetString("stories-heretic-heart-no-targets"), ent, ent);
            return;
        }

        var from = _transform.GetMapCoordinates(ent);
        var to = _transform.GetMapCoordinates(target);

        string text;
        if (from.MapId != to.MapId)
        {
            text = Loc.GetString("stories-heretic-heart-other-map", ("target", Name(target)));
        }
        else
        {
            var delta = to.Position - from.Position;
            var distance = delta.Length();
            var range = distance < args.NearDistance ? "near" : distance < args.MediumDistance ? "medium" : "far";
            text = Loc.GetString("stories-heretic-heart-direction",
                ("target", Name(target)),
                ("direction", Loc.GetString($"stories-heretic-direction-{delta.ToWorldAngle().GetDir()}")),
                ("range", Loc.GetString($"stories-heretic-range-{range}")));
        }

        _popup.PopupEntity(text, ent, ent, PopupType.Medium);
    }

    #endregion
}
```

Примечание: `CanBeSacrificed` использует `IsIncapacitated` — он покрывает и крит, и смерть.

- [ ] **Step 4: Теневой мир и связка с правилом**

`Content.Server/_Stories/Heretic/StoriesHereticSacrificeSystem.ShadowRealm.cs`:
```csharp
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server._Stories.Heretic;

public sealed partial class StoriesHereticSacrificeSystem
{
    [Dependency] private ITileDefinitionManager _tileDefs = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;

    private const string RealmTile = "FloorDark";
    private const int PlatformRadius = 4;
    private const int PlatformSpacing = 12;
    private static readonly Color RealmLight = Color.FromHex("#1b0f24");

    private Entity<MapGridComponent>? _realmGrid;
    private bool _realmFromFile;
    private int _nextPlatform;

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _realmGrid = null;
        _realmFromFile = false;
        _nextPlatform = 0;
    }

    private void SendToShadowRealm(EntityUid victim)
    {
        if (!TryEnsureRealm(out var grid))
            return;

        var center = Vector2i.Zero;
        if (!_realmFromFile)
        {
            center = new Vector2i(_nextPlatform++ * PlatformSpacing, 0);
            BuildPlatform(grid.Value, center);
        }

        var realm = EnsureComp<StoriesHereticShadowRealmComponent>(victim);
        realm.ReturnTime = _timing.CurTime + realm.Duration;
        realm.NextDamage = _timing.CurTime + realm.DamageInterval;

        _transform.SetCoordinates(victim, new EntityCoordinates(grid.Value, new Vector2(center.X + 0.5f, center.Y + 0.5f)));
    }

    private void ReturnFromRealm(Entity<StoriesHereticShadowRealmComponent> ent)
    {
        RemCompDeferred<StoriesHereticShadowRealmComponent>(ent);

        if (_rule.TryGetRandomStationCoordinates(out var coords))
        {
            _transform.SetCoordinates(ent.Owner, coords);
            _transform.AttachToGridOrMap(ent.Owner);
        }

        _popup.PopupEntity(Loc.GetString("stories-heretic-sacrifice-returned"), ent, ent, PopupType.Large);
    }

    private bool TryEnsureRealm([NotNullWhen(true)] out Entity<MapGridComponent>? grid)
    {
        if (_realmGrid is { } existing && !TerminatingOrDeleted(existing))
        {
            grid = existing;
            return true;
        }

        if (GetConfiguredRealmMap() is { } path
            && _mapLoader.TryLoadMap(path, out _, out var grids)
            && grids.Count > 0)
        {
            _realmGrid = grids.First();
            _realmFromFile = true;
        }
        else
        {
            var mapUid = _map.CreateMap(out var mapId);
            _atmosphere.SetMapAtmosphere(mapUid, false, CreateAir());

            var light = EnsureComp<MapLightComponent>(mapUid);
            light.AmbientLightColor = RealmLight;
            Dirty(mapUid, light);

            _realmGrid = _map.CreateGridEntity(mapId);
            _realmFromFile = false;
        }

        grid = _realmGrid;
        return grid != null;
    }

    private ResPath? GetConfiguredRealmMap()
    {
        var query = EntityQueryEnumerator<StoriesHereticRuleComponent>();
        while (query.MoveNext(out var rule))
        {
            if (rule.ShadowRealmMap is { } path)
                return path;
        }

        return null;
    }

    private void BuildPlatform(Entity<MapGridComponent> grid, Vector2i center)
    {
        var tile = new Tile(_tileDefs[RealmTile].TileId);
        var tiles = new List<(Vector2i, Tile)>();
        for (var x = -PlatformRadius; x <= PlatformRadius; x++)
        {
            for (var y = -PlatformRadius; y <= PlatformRadius; y++)
            {
                tiles.Add((center + new Vector2i(x, y), tile));
            }
        }

        _map.SetTiles(grid, tiles);
    }

    private static GasMixture CreateAir()
    {
        var air = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
        air.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
        air.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);
        return air;
    }
}
```

В `StoriesHereticRuleSystem.cs` добавить зависимость и вызов (в начало `OnAntagSelected`):
```csharp
    [Dependency] private StoriesHereticSacrificeSystem _sacrifice = default!;
```
```csharp
        if (TryComp<StoriesHereticComponent>(args.EntityUid, out var heretic))
            _sacrifice.AssignTargets((args.EntityUid, heretic));
```
Зависимости в начале класса держите по алфавиту: `_antag`, `_mind`, `_sacrifice`, `_turf`.

- [ ] **Step 5: YAML**

Добавить в `actions.yml`:
```yaml
- type: entity
  id: STActionHereticLivingHeart
  name: Living Heart
  description: Listen to your heart to find the next sacrifice target. Others can hear it beat.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    useDelay: 10
    checkCanInteract: false
  - type: Sprite
    sprite: Interface/Actions/devour.rsi
    layers:
    - state: icon
  - type: InstantAction
    event: !type:StoriesHereticLivingHeartEvent
```

Добавить в `rituals.yml`:
```yaml
- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualSacrifice
  name: Heartbeat of the Mansus
  description: Place an incapacitated, cuffed or sleeping target on the rune.
  components:
  - type: StoriesHereticRitual
    effect: Sacrifice

- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualReroll
  name: Relentless Heartbeat
  description: A poppy, a book and any clothing. Grants new sacrifice targets.
  components:
  - type: StoriesHereticRitual
    effect: RerollTargets
    ingredients:
    - name: stories-heretic-ingredient-poppy
      prototypes: [ FoodPoppy ]
    - name: stories-heretic-ingredient-book
      whitelist:
        tags: [ Book ]
    - name: stories-heretic-ingredient-clothing
      whitelist:
        components: [ Clothing ]
```

Добавить в `knowledge.yml`:
```yaml
- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeHeartbeat
  name: Heartbeat of the Mansus
  description: You can sacrifice your targets on a transmutation rune.
  components:
  - type: StoriesHereticKnowledge
    starting: true
    cost: 0
    rituals:
    - STHereticRitualSacrifice
    - STHereticRitualReroll

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeLivingHeart
  name: The Living Heart
  description: Your heart leads you to your targets.
  components:
  - type: StoriesHereticKnowledge
    starting: true
    cost: 0
    actions:
    - STActionHereticLivingHeart
```

- [ ] **Step 6: Локализация**

ru-RU:
```ftl
stories-heretic-sacrifice-not-target = Мансус не примет эту жертву.
stories-heretic-sacrifice-resisting = Жертва ещё сопротивляется.
stories-heretic-sacrifice-no-body = На руне нет жертвы.
stories-heretic-sacrifice-victim = Вас утягивает сквозь завесу реальности...
stories-heretic-sacrifice-returned = Вы возвращаетесь. Вы не помните, кто это сделал.
stories-heretic-ingredient-poppy = мак
stories-heretic-ingredient-clothing = одежда
stories-heretic-heart-no-targets = Ваше сердце молчит: целей нет.
stories-heretic-heart-other-map = { $target } — где-то далеко, за пределами этого места.
stories-heretic-heart-direction = { $target }: { $direction }, { $range }.
stories-heretic-range-near = рядом
stories-heretic-range-medium = неподалёку
stories-heretic-range-far = далеко
stories-heretic-direction-North = на севере
stories-heretic-direction-NorthEast = на северо-востоке
stories-heretic-direction-East = на востоке
stories-heretic-direction-SouthEast = на юго-востоке
stories-heretic-direction-South = на юге
stories-heretic-direction-SouthWest = на юго-западе
stories-heretic-direction-West = на западе
stories-heretic-direction-NorthWest = на северо-западе
stories-heretic-direction-Invalid = прямо здесь
```
en-US:
```ftl
stories-heretic-sacrifice-not-target = The Mansus will not accept this sacrifice.
stories-heretic-sacrifice-resisting = The victim is still resisting.
stories-heretic-sacrifice-no-body = There is no victim on the rune.
stories-heretic-sacrifice-victim = You are pulled through the veil of reality...
stories-heretic-sacrifice-returned = You return. You do not remember who did this.
stories-heretic-ingredient-poppy = poppy
stories-heretic-ingredient-clothing = clothing
stories-heretic-heart-no-targets = Your heart is silent: there are no targets.
stories-heretic-heart-other-map = { $target } is somewhere far away, beyond this place.
stories-heretic-heart-direction = { $target }: { $direction }, { $range }.
stories-heretic-range-near = near
stories-heretic-range-medium = not far
stories-heretic-range-far = far away
stories-heretic-direction-North = to the north
stories-heretic-direction-NorthEast = to the north-east
stories-heretic-direction-East = to the east
stories-heretic-direction-SouthEast = to the south-east
stories-heretic-direction-South = to the south
stories-heretic-direction-SouthWest = to the south-west
stories-heretic-direction-West = to the west
stories-heretic-direction-NorthWest = to the north-west
stories-heretic-direction-Invalid = right here
```

- [ ] **Step 7: Сборка, тесты, коммит**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test ... --filter "FullyQualifiedName~Heretic"` → PASS (в том числе `HereticSacrificeTest`, `HereticRuleTest`).
Run: YAML-линтер → без ошибок.
```bash
git add Content.Server/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Locale Content.IntegrationTests/Tests/_Stories/Heretic
git commit -m "feat(heretic): sacrifice, shadow realm, living heart"
```

---

### Task 7: Влияния и разорванная реальность

**Files:**
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticInfluenceComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/Components/StoriesHereticPiercedRealityComponent.cs`
- Create: `Content.Shared/_Stories/Heretic/StoriesHereticInfluenceSystem.cs`
- Create: `Content.Client/_Stories/Heretic/StoriesHereticInfluenceVisibilitySystem.cs`
- Modify: `Content.Shared/_Stories/Heretic/StoriesHereticSystem.Focus.cs` (метод `GetInfluenceMultiplier`)
- Modify: `Resources/Prototypes/_Stories/Heretic/entities.yml` (заменить заглушку влияния, добавить разорванную реальность)
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`

**Interfaces:**
- Consumes: `StoriesHereticSystem.AddPoints`, `StoriesHereticCodexComponent.InfluenceMultiplier`, `StoriesHereticHarvestDoAfterEvent`.
- Produces: `StoriesHereticSystem.GetInfluenceMultiplier(EntityUid uid) : int`; прототипы `STHereticInfluence`, `STHereticPiercedReality`.

- [ ] **Step 1: Компоненты**

`Content.Shared/_Stories/Heretic/Components/StoriesHereticInfluenceComponent.cs`:
```csharp
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Разлом реальности. Виден и доступен только еретикам.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticInfluenceComponent : Component
{
    [DataField]
    public TimeSpan HarvestTime = TimeSpan.FromSeconds(5);

    [DataField]
    public int Points = 1;

    [DataField]
    public EntProtoId PiercedPrototype = "STHereticPiercedReality";

    [DataField]
    public SoundSpecifier? HarvestSound = new SoundPathSpecifier("/Audio/Magic/staff_animation.ogg");
}
```

`Content.Shared/_Stories/Heretic/Components/StoriesHereticPiercedRealityComponent.cs`:
```csharp
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Heretic.Components;

/// <summary>
/// Собранное влияние: видно всем и опасно для не-еретиков.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoriesHereticPiercedRealityComponent : Component
{
    /// <summary>
    /// В SS14 нет урона мозгу, поэтому осмотр отнимает выносливость и вызывает галлюцинации.
    /// </summary>
    [DataField]
    public float ExamineStaminaDamage = 20f;

    [DataField]
    public TimeSpan ExamineHallucinationTime = TimeSpan.FromSeconds(5);

    [DataField]
    public float TouchChance = 0.25f;

    [DataField]
    public DamageSpecifier TouchDamage = new()
    {
        DamageDict = new() { { "Slash", 20 } },
    };
}
```

- [ ] **Step 2: Множитель Кодекса**

В `StoriesHereticSystem.Focus.cs` добавить метод:
```csharp
    /// <summary>
    /// Множитель очков за влияние: раскрытый Кодекс в руке удваивает их.
    /// </summary>
    public int GetInfluenceMultiplier(EntityUid uid)
    {
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            if (TryComp<StoriesHereticCodexComponent>(held, out var codex) && codex.Open)
                return codex.InfluenceMultiplier;
        }

        return 1;
    }
```

- [ ] **Step 3: Система влияний**

`Content.Shared/_Stories/Heretic/StoriesHereticInfluenceSystem.cs`:
```csharp
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticInfluenceSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly EntProtoId HallucinationEffect = "StatusEffectSeeingRainbow";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticInfluenceComponent, InteractHandEvent>(OnInfluenceInteract);
        SubscribeLocalEvent<StoriesHereticInfluenceComponent, StoriesHereticHarvestDoAfterEvent>(OnHarvest);
        SubscribeLocalEvent<StoriesHereticPiercedRealityComponent, ExaminedEvent>(OnPiercedExamined);
        SubscribeLocalEvent<StoriesHereticPiercedRealityComponent, InteractHandEvent>(OnPiercedInteract);
    }

    private void OnInfluenceInteract(Entity<StoriesHereticInfluenceComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || !HasComp<StoriesHereticComponent>(args.User))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.HarvestTime,
            new StoriesHereticHarvestDoAfterEvent(),
            ent,
            target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    private void OnHarvest(Entity<StoriesHereticInfluenceComponent> ent, ref StoriesHereticHarvestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (_net.IsClient || !TryComp<StoriesHereticComponent>(args.User, out var heretic))
            return;

        var coords = Transform(ent).Coordinates;
        _heretic.AddPoints((args.User, heretic), ent.Comp.Points * _heretic.GetInfluenceMultiplier(args.User));
        _audio.PlayPvs(ent.Comp.HarvestSound, coords);
        Spawn(ent.Comp.PiercedPrototype, coords);
        QueueDel(ent);
    }

    private void OnPiercedExamined(Entity<StoriesHereticPiercedRealityComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<StoriesHereticComponent>(args.Examiner))
        {
            args.PushMarkup(Loc.GetString("stories-heretic-pierced-examine-heretic"));
            return;
        }

        args.PushMarkup(Loc.GetString("stories-heretic-pierced-examine"));
        if (_net.IsClient)
            return;

        _stamina.TakeStaminaDamage(args.Examiner, ent.Comp.ExamineStaminaDamage, source: ent);
        _statusEffects.TryUpdateStatusEffectDuration(args.Examiner, HallucinationEffect, ent.Comp.ExamineHallucinationTime);
    }

    private void OnPiercedInteract(Entity<StoriesHereticPiercedRealityComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || HasComp<StoriesHereticComponent>(args.User))
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString("stories-heretic-pierced-touch"), args.User, args.User, PopupType.MediumCaution);
        if (_random.Prob(ent.Comp.TouchChance))
            _damageable.TryChangeDamage(args.User, ent.Comp.TouchDamage, true);
    }
}
```

- [ ] **Step 4: Клиент — видимость только для еретика**

`Content.Client/_Stories/Heretic/StoriesHereticInfluenceVisibilitySystem.cs`:
```csharp
using Content.Shared._Stories.Heretic.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Stories.Heretic;

/// <summary>
/// Влияния скрыты (в YAML <c>visible: false</c>) и показываются, только если локальный игрок — еретик.
/// Компонент еретика приходит только его владельцу (см. <c>OnGetStateAttempt</c>), поэтому другим он не виден.
/// </summary>
public sealed partial class StoriesHereticInfluenceVisibilitySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticInfluenceComponent, ComponentStartup>(OnInfluenceStartup);
        SubscribeLocalEvent<StoriesHereticComponent, ComponentStartup>(OnHereticStartup);
        SubscribeLocalEvent<StoriesHereticComponent, ComponentShutdown>(OnHereticShutdown);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnInfluenceStartup(Entity<StoriesHereticInfluenceComponent> ent, ref ComponentStartup args)
    {
        _sprite.SetVisible(ent.Owner, IsLocalHeretic());
    }

    private void OnHereticStartup(Entity<StoriesHereticComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner)
            SetAllVisible(true);
    }

    private void OnHereticShutdown(Entity<StoriesHereticComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            SetAllVisible(false);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        SetAllVisible(IsLocalHeretic());
    }

    private bool IsLocalHeretic()
    {
        return _player.LocalEntity is { } local && HasComp<StoriesHereticComponent>(local);
    }

    private void SetAllVisible(bool visible)
    {
        var query = EntityQueryEnumerator<StoriesHereticInfluenceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            _sprite.SetVisible((uid, sprite), visible);
        }
    }
}
```

- [ ] **Step 5: YAML**

В `entities.yml` заменить заглушку `STHereticInfluence` целиком и добавить разорванную реальность:
```yaml
- type: entity
  id: STHereticInfluence
  name: reality smash
  description: A tear in the fabric of reality. Something whispers from the other side.
  categories: [ HideSpawnMenu ]
  placement:
    mode: SnapgridCenter
  components:
  - type: StoriesHereticInfluence
  - type: Sprite
    sprite: Structures/Magic/Cult/rune.rsi
    state: cult7
    color: "#9b59d0"
    visible: false
    drawdepth: Effects
  - type: Clickable
  - type: InteractionOutline
  - type: Transform
    anchored: true

- type: entity
  id: STHereticPiercedReality
  name: pierced reality
  description: A wound in the world. Looking at it hurts.
  categories: [ HideSpawnMenu ]
  placement:
    mode: SnapgridCenter
  components:
  - type: StoriesHereticPiercedReality
  - type: Sprite
    sprite: Structures/Magic/Cult/rune.rsi
    state: cult7
    color: "#c0392b"
    drawdepth: Effects
  - type: Clickable
  - type: InteractionOutline
  - type: Transform
    anchored: true
```

- [ ] **Step 6: Локализация**

ru-RU:
```ftl
stories-heretic-pierced-examine = [color=#c0392b]От одного взгляда на это кружится голова.[/color]
stories-heretic-pierced-examine-heretic = Вы уже испили знание из этого разлома.
stories-heretic-pierced-touch = Что-то по ту сторону хватает вас!
```
en-US:
```ftl
stories-heretic-pierced-examine = [color=#c0392b]Just looking at it makes your head spin.[/color]
stories-heretic-pierced-examine-heretic = You have already drunk the knowledge from this tear.
stories-heretic-pierced-touch = Something on the other side grabs you!
```

- [ ] **Step 7: Сборка, тесты, линтер, коммит**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test ... --filter "FullyQualifiedName~Heretic"` → PASS.
Run: YAML-линтер → без ошибок.
```bash
git add Content.Shared/_Stories/Heretic Content.Client/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Locale
git commit -m "feat(heretic): influences and pierced reality"
```

---

### Task 8: Окно дерева знаний

**Files:**
- Create: `Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeNode.cs`
- Create: `Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeTreeControl.cs`
- Create: `Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeWindow.xaml`
- Create: `Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeWindow.xaml.cs`
- Create: `Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeBoundUserInterface.cs`
- Create: `Content.Client/_Stories/Heretic/StoriesHereticKnowledgeUISystem.cs`
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`

**Interfaces:**
- Consumes: `StoriesHereticSystem.AllKnowledge`, `StoriesHereticSystem.CanLearn`, `StoriesHereticKnowledgeUiKey`, `StoriesHereticLearnMessage` (Task 1). Имя класса BUI строго `StoriesHereticKnowledgeBoundUserInterface` — его строкой регистрирует `StoriesHereticSystem.OnMapInit`.
- Produces: окно; ничего, на что опираются другие задачи.

- [ ] **Step 1: Модель узла**

`Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeNode.cs`:
```csharp
namespace Content.Client._Stories.Heretic.UI;

public enum StoriesHereticNodeState : byte
{
    Learned,
    Available,
    Locked,
}

public sealed record StoriesHereticKnowledgeNode(
    string Id,
    string Name,
    string Description,
    int Cost,
    int Column,
    int Stage,
    IReadOnlyList<string> Requires,
    StoriesHereticNodeState State,
    string? Reason);
```

- [ ] **Step 2: Раскладка дерева**

`Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeTreeControl.cs`:
```csharp
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Stories.Heretic.UI;

/// <summary>
/// Раскладывает узлы знаний по колонкам (пути) и строкам (ступени) и рисует линии связей.
/// Узлы создаются из данных, поэтому их нельзя описать в XAML.
/// </summary>
public sealed class StoriesHereticKnowledgeTreeControl : LayoutContainer
{
    private const float ColumnWidth = 210f;
    private const float RowHeight = 70f;
    private const float NodeWidth = 190f;
    private const float NodeHeight = 44f;
    private const float HeaderHeight = 30f;

    private static readonly Color LearnedColor = Color.FromHex("#58a65c");
    private static readonly Color LineColor = Color.FromHex("#666666");

    private readonly Dictionary<string, Control> _buttons = new();
    private IReadOnlyList<StoriesHereticKnowledgeNode> _nodes = Array.Empty<StoriesHereticKnowledgeNode>();

    public event Action<string>? OnLearn;

    public void SetNodes(IReadOnlyList<string> columns, IReadOnlyList<StoriesHereticKnowledgeNode> nodes)
    {
        RemoveAllChildren();
        _buttons.Clear();
        _nodes = nodes;

        for (var i = 0; i < columns.Count; i++)
        {
            var header = new Label
            {
                Text = columns[i],
                Align = Label.AlignMode.Center,
                SetWidth = NodeWidth,
            };
            AddChild(header);
            SetPosition(header, new Vector2(i * ColumnWidth, 0));
        }

        var maxStage = 0;
        foreach (var node in nodes)
        {
            var button = new Button
            {
                Text = Loc.GetString("stories-heretic-ui-node", ("name", node.Name), ("cost", node.Cost)),
                ToolTip = node.Reason == null ? node.Description : $"{node.Description}\n\n{node.Reason}",
                Disabled = node.State != StoriesHereticNodeState.Available,
                SetSize = new Vector2(NodeWidth, NodeHeight),
                ClipText = true,
            };

            if (node.State == StoriesHereticNodeState.Learned)
                button.ModulateSelfOverride = LearnedColor;

            var id = node.Id;
            button.OnPressed += _ => OnLearn?.Invoke(id);

            AddChild(button);
            SetPosition(button, new Vector2(node.Column * ColumnWidth, HeaderHeight + node.Stage * RowHeight));
            _buttons[id] = button;
            maxStage = Math.Max(maxStage, node.Stage);
        }

        MinSize = new Vector2(columns.Count * ColumnWidth, HeaderHeight + (maxStage + 1) * RowHeight);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var node in _nodes)
        {
            if (!_buttons.TryGetValue(node.Id, out var to))
                continue;

            foreach (var required in node.Requires)
            {
                if (!_buttons.TryGetValue(required, out var from))
                    continue;

                var start = (from.Position + new Vector2(NodeWidth / 2, NodeHeight)) * UIScale;
                var end = (to.Position + new Vector2(NodeWidth / 2, 0)) * UIScale;
                handle.DrawLine(start, end, node.State == StoriesHereticNodeState.Learned ? LearnedColor : LineColor);
            }
        }
    }
}
```

- [ ] **Step 3: Окно**

`Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeWindow.xaml`:
```xml
<controls:FancyWindow
    xmlns="https://spacestation14.io"
    xmlns:controls="clr-namespace:Content.Client.UserInterface.Controls"
    xmlns:ui="clr-namespace:Content.Client._Stories.Heretic.UI"
    Title="{Loc stories-heretic-ui-title}"
    MinSize="640 480"
    SetSize="900 640">
    <BoxContainer Orientation="Vertical" Margin="8" SeparationOverride="6">
        <BoxContainer Orientation="Horizontal" SeparationOverride="16">
            <Label Name="PointsLabel" />
            <Label Name="PathLabel" />
        </BoxContainer>
        <Label Name="RitualLabel" FontColorOverride="#8fd18f" />
        <ScrollContainer VerticalExpand="True" HorizontalExpand="True">
            <ui:StoriesHereticKnowledgeTreeControl Name="Tree" />
        </ScrollContainer>
    </BoxContainer>
</controls:FancyWindow>
```

`Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeWindow.xaml.cs`:
```csharp
using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Stories.Heretic.UI;

[GenerateTypedNameReferences]
public sealed partial class StoriesHereticKnowledgeWindow : FancyWindow
{
    public event Action<string>? OnLearn;

    public StoriesHereticKnowledgeWindow()
    {
        RobustXamlLoader.Load(this);
        Tree.OnLearn += id => OnLearn?.Invoke(id);
    }

    public void UpdateState(int points,
        string path,
        string? ritualItems,
        IReadOnlyList<string> columns,
        IReadOnlyList<StoriesHereticKnowledgeNode> nodes)
    {
        PointsLabel.Text = Loc.GetString("stories-heretic-ui-points", ("points", points));
        PathLabel.Text = Loc.GetString("stories-heretic-ui-path", ("path", path));

        RitualLabel.Visible = ritualItems != null;
        if (ritualItems != null)
            RitualLabel.Text = Loc.GetString("stories-heretic-ui-ritual", ("items", ritualItems));

        Tree.SetNodes(columns, nodes);
    }
}
```

- [ ] **Step 4: BUI (логика сборки узлов)**

`Content.Client/_Stories/Heretic/UI/StoriesHereticKnowledgeBoundUserInterface.cs`:
```csharp
using System.Linq;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Stories.Heretic.UI;

/// <summary>
/// Собирает узлы дерева из прототипов знаний и <see cref="StoriesHereticComponent"/> владельца.
/// </summary>
[UsedImplicitly]
public sealed class StoriesHereticKnowledgeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _proto = default!;

    [ViewVariables]
    private StoriesHereticKnowledgeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StoriesHereticKnowledgeWindow>();
        _window.OnLearn += id => SendMessage(new StoriesHereticLearnMessage(id));
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent(Owner, out StoriesHereticComponent? heretic))
            return;

        var system = EntMan.System<StoriesHereticSystem>();
        var ent = new Entity<StoriesHereticComponent>(Owner, heretic);

        // Колонка 0 — общие знания, далее пути в алфавитном порядке.
        var paths = system.AllKnowledge
            .Where(k => k.Comp.Path != null)
            .Select(k => k.Comp.Path!.Value.Id)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        var columns = new List<string> { Loc.GetString("stories-heretic-ui-column-general") };
        columns.AddRange(paths.Select(p => Loc.GetString($"stories-heretic-path-{p}")));

        var nodes = new List<StoriesHereticKnowledgeNode>();
        foreach (var (id, knowledge) in system.AllKnowledge)
        {
            if (knowledge.Starting || !_proto.TryIndex(id, out var proto))
                continue;

            string? reason = null;
            var state = heretic.Knowledge.Contains(id)
                ? StoriesHereticNodeState.Learned
                : system.CanLearn(ent, id, out reason)
                    ? StoriesHereticNodeState.Available
                    : StoriesHereticNodeState.Locked;

            var column = knowledge.Path is { } path ? paths.IndexOf(path.Id) + 1 : 0;

            nodes.Add(new StoriesHereticKnowledgeNode(id,
                proto.Name,
                proto.Description,
                knowledge.Cost,
                column,
                knowledge.Stage,
                knowledge.Requires.Select(r => r.Id).ToList(),
                state,
                state == StoriesHereticNodeState.Learned ? null : reason));
        }

        var pathName = heretic.Path is { } chosen
            ? Loc.GetString($"stories-heretic-path-{chosen.Id}")
            : Loc.GetString("stories-heretic-path-none");

        var ritualItems = heretic.KnowledgeRitualUsed
            ? null
            : string.Join(", ", heretic.KnowledgeRitualTags.Select(t => Loc.GetString($"stories-heretic-tag-{t.Id}")));

        _window.UpdateState(heretic.Points, pathName, ritualItems, columns, nodes);
    }
}
```

- [ ] **Step 5: Обновление окна при изменении компонента**

`Content.Client/_Stories/Heretic/StoriesHereticKnowledgeUISystem.cs`:
```csharp
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;

namespace Content.Client._Stories.Heretic;

public sealed partial class StoriesHereticKnowledgeUISystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(Entity<StoriesHereticComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (_ui.TryGetOpenUi(ent.Owner, StoriesHereticKnowledgeUiKey.Key, out var bui))
                bui.Update();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to update heretic knowledge window for {ToPrettyString(ent)}: {e}");
        }
    }
}
```

- [ ] **Step 6: Локализация**

ru-RU:
```ftl
stories-heretic-ui-title = Знания Мансуса
stories-heretic-ui-points = Очки знаний: { $points }
stories-heretic-ui-path = Путь: { $path }
stories-heretic-ui-ritual = Ритуал знаний требует: { $items }
stories-heretic-ui-column-general = Общие
stories-heretic-ui-node = { $name } ({ $cost })
```
en-US:
```ftl
stories-heretic-ui-title = Mansus knowledge
stories-heretic-ui-points = Knowledge points: { $points }
stories-heretic-ui-path = Path: { $path }
stories-heretic-ui-ritual = The Ritual of Knowledge requires: { $items }
stories-heretic-ui-column-general = General
stories-heretic-ui-node = { $name } ({ $cost })
```

- [ ] **Step 7: Сборка и ручная проверка**

Run: `dotnet build -c Debug` → `0 Error(s)`.

Ручная проверка (окно — UI, автотестом его не проверить):
1. `.\runserver.bat`, затем `.\runclient.bat`, зайти в раунд за любую профессию.
2. В консоли (`~`): `addcomp $SELF StoriesHeretic`. Если `$SELF` не подставляется, узнать свой uid командой `getuid`/через F7 (VV) и подставить число.
3. На панели действий появились «Знания Мансуса», «Хватка Мансуса», «Начертить руну», «Живое сердце».
4. «Знания Мансуса» открывает окно: видны очки (1), путь «не выбран», строка ритуала знаний с 4 предметами. Колонка «Общие» пока пустая (знаний пути ещё нет — появятся в Task 9).

- [ ] **Step 8: Коммит**

```bash
git add Content.Client/_Stories/Heretic Resources/Locale
git commit -m "feat(heretic): knowledge tree window"
```

---

### Task 9: Путь Ash — знания, клинок, хватка, рывок, метка, вулкан, огненный клинок, перерождение

**Files:**
- Create: `Content.Server/_Stories/Heretic/Ash/StoriesHereticAshMarkComponent.cs`
- Create: `Content.Server/_Stories/Heretic/Ash/StoriesHereticBladeFireComponent.cs`
- Create: `Content.Server/_Stories/Heretic/Ash/StoriesHereticAshSystem.cs`
- Create: `Resources/Prototypes/_Stories/Heretic/knowledge_ash.yml`, `actions_ash.yml`, `rituals_ash.yml`, `entities_ash.yml`, `ash_misc.yml`
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`

**Interfaces:**
- Consumes: `StoriesHereticGraspSystem.ApplyMark` (Task 5), `StoriesHereticMarkTriggeredEvent`, `StoriesHereticBladeHitEvent`, события действий `StoriesHereticVolcanoBlastEvent`, `StoriesHereticRebirthEvent` (Task 1), `StoriesHereticRequiresFocusComponent` (Task 4), `StoriesHereticGraspBlindComponent`, `StoriesHereticGraspMarkComponent`, `StoriesHereticBladeComponent` (Task 5).
- Produces: `StoriesHereticAshSystem` (partial; Task 10 добавит `.Ascension.cs` и вызовет `InitializeAscension()` / `UpdateAscension()`); знание-родитель Вознесения ссылается на `STHereticKnowledgeAshRebirth`.

- [ ] **Step 1: Компоненты Ash (сервер)**

Компоненты используются только сервером; клиент пропускает их в YAML (`IgnoreMissingComponents` в `Content.Client/Entry/EntryPoint.cs`).

`Content.Server/_Stories/Heretic/Ash/StoriesHereticAshMarkComponent.cs`:
```csharp
namespace Content.Server._Stories.Heretic.Ash;

/// <summary>
/// На еретике: параметры срабатывания метки Пепла.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticAshMarkComponent : Component
{
    [DataField]
    public float StaminaDamage = 20f;

    [DataField]
    public float BaseFireStacks = 2f;

    [DataField]
    public float SpreadRadius = 2f;

    [DataField]
    public int MaxHops = 3;

    [DataField]
    public TimeSpan SpreadMarkDuration = TimeSpan.FromSeconds(15);
}
```

`Content.Server/_Stories/Heretic/Ash/StoriesHereticBladeFireComponent.cs`:
```csharp
namespace Content.Server._Stories.Heretic.Ash;

/// <summary>
/// На еретике: удары клинком еретика поджигают.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticBladeFireComponent : Component
{
    [DataField]
    public float FireStacks = 1f;
}
```

- [ ] **Step 2: Система Ash**

`Content.Server/_Stories/Heretic/Ash/StoriesHereticAshSystem.cs`:
```csharp
using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ash;

public sealed partial class StoriesHereticAshSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private StoriesHereticGraspSystem _grasp = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> AshPath = "STHereticPathAsh";

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticMarkTriggeredEvent>(OnMarkTriggered);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticVolcanoBlastEvent>(OnVolcanoBlast);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticRebirthEvent>(OnRebirth);
        SubscribeLocalEvent<StoriesHereticBladeFireComponent, StoriesHereticBladeHitEvent>(OnBladeFireHit);
    }

    private void Ignite(EntityUid target, float stacks)
    {
        if (TryComp<FlammableComponent>(target, out var flammable))
            _flammable.AdjustFireStacks(target, stacks, flammable, ignite: true);
    }

    private bool IsBurning(EntityUid uid)
    {
        return TryComp<FlammableComponent>(uid, out var flammable) && flammable.OnFire;
    }

    private void OnMarkTriggered(ref StoriesHereticMarkTriggeredEvent args)
    {
        if (args.Path != AshPath || !TryComp<StoriesHereticAshMarkComponent>(args.Heretic, out var config))
            return;

        _stamina.TakeStaminaDamage(args.Target, config.StaminaDamage, source: args.Heretic);
        Ignite(args.Target, config.BaseFireStacks + args.Hops);

        if (args.Hops >= config.MaxHops)
            return;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(args.Target).Coordinates, config.SpreadRadius, _mobs);
        foreach (var mob in _mobs)
        {
            if (mob.Owner == args.Target || HasComp<StoriesHereticComponent>(mob))
                continue;

            _grasp.ApplyMark(mob, AshPath, config.SpreadMarkDuration, args.Hops + 1);
        }
    }

    private void OnVolcanoBlast(Entity<StoriesHereticComponent> ent, ref StoriesHereticVolcanoBlastEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var current = ent.Owner;
        var visited = new HashSet<EntityUid> { ent.Owner };
        for (var i = 0; i < args.Bounces; i++)
        {
            if (FindNearestUnburnt(current, args.Range, visited) is not { } next)
                break;

            visited.Add(next);
            _damageable.TryChangeDamage(next, args.Damage, origin: ent);
            Ignite(next, args.FireStacks);
            _damageable.TryChangeDamage(ent.Owner, args.SelfHeal, true);
            current = next;
        }
    }

    private EntityUid? FindNearestUnburnt(EntityUid from, float range, HashSet<EntityUid> exclude)
    {
        var origin = _transform.GetMapCoordinates(from).Position;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(from).Coordinates, range, _mobs);

        EntityUid? best = null;
        var bestDistance = float.MaxValue;
        foreach (var mob in _mobs)
        {
            if (exclude.Contains(mob)
                || HasComp<StoriesHereticComponent>(mob)
                || !_mobState.IsAlive(mob)
                || IsBurning(mob))
                continue;

            var distance = (_transform.GetMapCoordinates(mob).Position - origin).LengthSquared();
            if (distance >= bestDistance)
                continue;

            best = mob;
            bestDistance = distance;
        }

        return best;
    }

    private void OnRebirth(Entity<StoriesHereticComponent> ent, ref StoriesHereticRebirthEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, args.Range, _mobs);

        var drained = 0;
        foreach (var mob in _mobs)
        {
            if (mob.Owner == ent.Owner || !IsBurning(mob) || _mobState.IsDead(mob))
                continue;

            drained++;
            _damageable.TryChangeDamage(ent.Owner, args.HealPerTarget, true);

            if (_mobState.IsCritical(mob))
                _damageable.TryChangeDamage(mob.Owner, args.FinishDamage, true, origin: ent);
        }

        _flammable.Extinguish(ent);
        _popup.PopupEntity(Loc.GetString("stories-heretic-ash-rebirth", ("count", drained)), ent, ent);
    }

    private void OnBladeFireHit(Entity<StoriesHereticBladeFireComponent> ent, ref StoriesHereticBladeHitEvent args)
    {
        Ignite(args.Target, ent.Comp.FireStacks);
    }
}
```

- [ ] **Step 3: YAML пути**

`Resources/Prototypes/_Stories/Heretic/ash_misc.yml`:
```yaml
- type: polymorph
  id: STHereticAshJaunt
  configuration:
    entity: EtherealJaunt
    transferName: true
    inventory: None
    forced: true
    revertOnDeath: true
    revertOnCrit: true
    allowRepeatedMorphs: false
    polymorphSound: /Audio/Magic/ethereal_enter.ogg
    exitPolymorphSound: /Audio/Magic/ethereal_exit.ogg
    duration: 3
```

`Resources/Prototypes/_Stories/Heretic/actions_ash.yml`:
```yaml
- type: entity
  id: STActionHereticAshPassage
  name: Ashen Passage
  description: Dissolve into ash for a moment and pass through walls. Works even while cuffed.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    useDelay: 15
    checkCanInteract: false
  - type: Sprite
    sprite: Objects/Magic/magicactions.rsi
    layers:
    - state: jaunt
  - type: InstantAction
    event: !type:PolymorphActionEvent
      protoId: STHereticAshJaunt
  - type: StoriesHereticRequiresFocus

- type: entity
  id: STActionHereticVolcanoBlast
  name: Volcano Blast
  description: A beam of fire that jumps between creatures that are not yet burning.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    useDelay: 30
  - type: Sprite
    sprite: Objects/Magic/magicactions.rsi
    layers:
    - state: fireball
  - type: InstantAction
    event: !type:StoriesHereticVolcanoBlastEvent
      damage:
        types:
          Heat: 10
      selfHeal:
        types:
          Heat: -5
  - type: StoriesHereticRequiresFocus

- type: entity
  id: STActionHereticRebirth
  name: Nightwatcher's Rebirth
  description: Drain life from everyone burning around you. Burning victims in critical condition die.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    useDelay: 60
  - type: Sprite
    sprite: Objects/Magic/magicactions.rsi
    layers:
    - state: repulse
  - type: InstantAction
    event: !type:StoriesHereticRebirthEvent
      healPerTarget:
        groups:
          Brute: -10
          Burn: -10
          Toxin: -10
          Airloss: -10
      finishDamage:
        types:
          Heat: 300
  - type: StoriesHereticRequiresFocus
```

`Resources/Prototypes/_Stories/Heretic/entities_ash.yml`:
```yaml
- type: entity
  parent: BaseKnife
  id: STHereticBladeAsh
  name: ashen blade
  description: A curved blade that smells of smoke. Breaking it carries you away through the Mansus.
  components:
  - type: StoriesHereticBlade
  - type: Sprite
    sprite: Objects/Weapons/Melee/cult_dagger.rsi
    state: icon
    color: "#d9792b"
  - type: Item
    sprite: Objects/Weapons/Melee/cult_dagger.rsi
    size: Normal
  - type: MeleeWeapon
    wideAnimationRotation: -135
    attackRate: 1.2
    damage:
      types:
        Slash: 17
    soundHit:
      path: /Audio/Weapons/bladeslice.ogg
```

`Resources/Prototypes/_Stories/Heretic/rituals_ash.yml`:
```yaml
- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualAshBlade
  name: Ashen Blade
  description: A knife and a match or lighter.
  components:
  - type: StoriesHereticRitual
    limit: 3
    ingredients:
    - name: stories-heretic-ingredient-knife
      whitelist:
        tags: [ Knife ]
    - name: stories-heretic-ingredient-fire
      whitelist:
        tags: [ Matchstick ]
      prototypes: [ Lighter ]
    results:
    - STHereticBladeAsh
```

`Resources/Prototypes/_Stories/Heretic/knowledge_ash.yml`:
```yaml
- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshRoot
  name: Nightwatcher's Secret
  description: Step onto the Path of Ash. Learn to forge ashen blades from a knife and a flame.
  components:
  - type: StoriesHereticKnowledge
    path: STHereticPathAsh
    choosesPath: true
    stage: 1
    rituals:
    - STHereticRitualAshBlade

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshGrasp
  name: Grasp of Ash
  description: Your Mansus Grasp blinds its victim.
  components:
  - type: StoriesHereticKnowledge
    path: STHereticPathAsh
    stage: 2
    requires: [ STHereticKnowledgeAshRoot ]
    addComponents:
    - type: StoriesHereticGraspBlind
      duration: 5

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshPassage
  name: Ashen Passage
  description: A short jaunt through walls. Works while cuffed.
  components:
  - type: StoriesHereticKnowledge
    path: STHereticPathAsh
    stage: 3
    requires: [ STHereticKnowledgeAshGrasp ]
    actions:
    - STActionHereticAshPassage

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshMark
  name: Mark of Ash
  description: Your Grasp marks victims. A blade strike on a marked victim sets them ablaze, and the mark spreads to those nearby.
  components:
  - type: StoriesHereticKnowledge
    path: STHereticPathAsh
    stage: 4
    cost: 2
    requires: [ STHereticKnowledgeAshPassage ]
    addComponents:
    - type: StoriesHereticGraspMark
      path: STHereticPathAsh
    - type: StoriesHereticAshMark

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshVolcano
  name: Volcano Blast
  description: A beam of fire that jumps between creatures.
  components:
  - type: StoriesHereticKnowledge
    path: STHereticPathAsh
    stage: 5
    requires: [ STHereticKnowledgeAshMark ]
    actions:
    - STActionHereticVolcanoBlast

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshFieryBlade
  name: Fiery Blade
  description: Your blades set their victims on fire.
  components:
  - type: StoriesHereticKnowledge
    path: STHereticPathAsh
    stage: 6
    cost: 2
    requires: [ STHereticKnowledgeAshVolcano ]
    addComponents:
    - type: StoriesHereticBladeFire

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshRebirth
  name: Nightwatcher's Rebirth
  description: Drain life from everyone burning around you.
  components:
  - type: StoriesHereticKnowledge
    path: STHereticPathAsh
    stage: 7
    requires: [ STHereticKnowledgeAshFieryBlade ]
    actions:
    - STActionHereticRebirth
```

- [ ] **Step 4: Локализация**

ru-RU:
```ftl
stories-heretic-ash-rebirth = Вы вытягиваете жизнь из пламени. Жертв: { $count }.
stories-heretic-ingredient-fire = спичка или зажигалка
```
en-US:
```ftl
stories-heretic-ash-rebirth = You drain life from the flames. Victims: { $count }.
stories-heretic-ingredient-fire = matchstick or lighter
```

- [ ] **Step 5: Тест — клинок Пепла создаётся ритуалом**

Добавить тест в `Content.IntegrationTests/Tests/_Stories/Heretic/HereticRitualTest.cs` (внутрь класса):
```csharp
    [Test]
    public async Task AshBladeRitual()
    {
        var testMap = await Pair.CreateTestMap();
        var heretics = Server.System<StoriesHereticSystem>();
        var rituals = Server.System<StoriesHereticRitualSystem>();

        await Server.WaitAssertion(() =>
        {
            var coords = testMap.GridCoords;
            var uid = SSpawnAtPosition("MobHuman", coords);
            var comp = SEntMan.AddComponent<StoriesHereticComponent>(uid);
            var heretic = new Entity<StoriesHereticComponent>(uid, comp);

            Assert.That(heretics.TryLearn(heretic, "STHereticKnowledgeAshRoot"), Is.True);
            Assert.That(comp.Rituals, Does.Contain((EntProtoId) "STHereticRitualAshBlade"));

            var rune = SSpawnAtPosition("STHereticRune", coords);
            SSpawnAtPosition("KitchenKnife", coords);
            SSpawnAtPosition("Lighter", coords);

            Assert.That(rituals.TryPerform(heretic, rune), Is.True);
            Assert.That(heretics.CountLimitedResults(heretic, "STHereticRitualAshBlade"), Is.EqualTo(1));
        });
    }
```
(`using Robust.Shared.Prototypes;` уже есть в файле с Task 3.)

- [ ] **Step 6: Сборка, тесты, линтер**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test ... --filter "FullyQualifiedName~Heretic"` → PASS.
Run: YAML-линтер → без ошибок.

- [ ] **Step 7: Ручная проверка**

На локальном сервере (см. Task 8, Step 7): `addcomp` еретика, затем:
1. Окно знаний: в колонке «Пепел» 7 узлов; «Секрет Ночного дозорного» доступен (1 очко).
2. Изучить корень → путь «Пепел», очки 0. Админ-командой выдать очки нельзя без кода, поэтому для проверки остальных узлов через VV (F7) на своём теле выставьте `StoriesHereticComponent.Points = 20`.
3. Нарисовать руну (ручка в руке), положить нож + зажигалку, кликнуть по руне пустой рукой → появился пепельный клинок. Четвёртый клинок не создаётся.
4. Изучить «Хватку Пепла» → Хватка ослепляет (заспавнить `MobHuman`, применить).
5. Изучить «Пепельный проход» без фокуса → отказ «Без фокуса...». Создать Янтарный фокус ритуалом (лист стекла + глаза) или заспавнить `STHereticAmberFocus`, надеть на шею → рывок работает.
6. «Метка Пепла» → Хватка + удар клинком поджигает цель.
7. «Извержение вулкана», «Огненный клинок», «Перерождение» — применить на заспавненных мобах.

- [ ] **Step 8: Коммит**

```bash
git add Content.Server/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Locale Content.IntegrationTests/Tests/_Stories/Heretic
git commit -m "feat(heretic): Path of Ash"
```

---

### Task 10: Пепельные глаза, Маска безумия, Вознесение Ash

**Files:**
- Create: `Content.Server/_Stories/Heretic/Ash/StoriesHereticMadnessMaskComponent.cs`
- Create: `Content.Server/_Stories/Heretic/Ash/StoriesHereticFireRingComponent.cs`
- Create: `Content.Server/_Stories/Heretic/Ash/StoriesHereticAshAscensionRitualComponent.cs`
- Create: `Content.Server/_Stories/Heretic/Ash/StoriesHereticAshSystem.Ascension.cs`
- Modify: `Content.Server/_Stories/Heretic/Ash/StoriesHereticAshSystem.cs` (вызов `InitializeAscension`, `Update`)
- Modify: `Resources/Prototypes/_Stories/Heretic/knowledge_ash.yml`, `actions_ash.yml`, `rituals_ash.yml`, `entities_ash.yml`, `ash_misc.yml`
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`

**Interfaces:**
- Consumes: `StoriesHereticSystem.SetAscended`, `StoriesHereticRitualEffectEvent` (эффект `Ascension`), `StoriesHereticFireRingEvent`, `StoriesHereticFireWaveEvent` (Task 1), `StoriesHereticFocusComponent` (Task 4).
- Produces: ничего для других задач.

- [ ] **Step 1: Компоненты**

`Content.Server/_Stories/Heretic/Ash/StoriesHereticMadnessMaskComponent.cs`:
```csharp
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Ash;

/// <summary>
/// Маска безумия: пока её носит еретик, окружающие теряют выносливость и галлюцинируют.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticMadnessMaskComponent : Component
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(2);

    [DataField]
    public float StaminaDamage = 5f;

    [DataField]
    public TimeSpan HallucinationTime = TimeSpan.FromSeconds(4);

    [DataField]
    public EntityUid? Wearer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
```

`Content.Server/_Stories/Heretic/Ash/StoriesHereticFireRingComponent.cs`:
```csharp
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Ash;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticFireRingComponent : Component
{
    [DataField]
    public float Radius = 2f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public float FireStacks = 2f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
```

`Content.Server/_Stories/Heretic/Ash/StoriesHereticAshAscensionRitualComponent.cs`:
```csharp
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Heretic.Ash;

/// <summary>
/// Данные ритуала Вознесения Ash. Живёт на прототипе ритуала.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticAshSystem))]
public sealed partial class StoriesHereticAshAscensionRitualComponent : Component
{
    [DataField]
    public int Corpses = 3;

    [DataField]
    public FixedPoint2 HeatThreshold = 100;

    [DataField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public ProtoId<DamageModifierSetPrototype> DamageModifierSet = "STHereticAshAscended";

    [DataField]
    public LocId Announcement = "stories-heretic-ash-ascension-announcement";

    [DataField]
    public Color AnnouncementColor = Color.OrangeRed;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Ambience/Antag/ling_alert.ogg");
}
```

- [ ] **Step 2: Вознесение, кольцо, волна, маска**

`Content.Server/_Stories/Heretic/Ash/StoriesHereticAshSystem.Ascension.cs`:
```csharp
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Heretic.Ash;

public sealed partial class StoriesHereticAshSystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly EntProtoId HallucinationEffect = "StatusEffectSeeingRainbow";
    private static readonly EntProtoId PressureImmunityEffect = "StatusEffectPressureImmunity";

    private void InitializeAscension()
    {
        SubscribeLocalEvent<StoriesHereticRitualEffectEvent>(OnRitualEffect);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticFireRingEvent>(OnFireRing);
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticFireWaveEvent>(OnFireWave);
        SubscribeLocalEvent<StoriesHereticMadnessMaskComponent, GotEquippedEvent>(OnMaskEquipped);
        SubscribeLocalEvent<StoriesHereticMadnessMaskComponent, GotUnequippedEvent>(OnMaskUnequipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var rings = EntityQueryEnumerator<StoriesHereticFireRingComponent>();
        while (rings.MoveNext(out var uid, out var ring))
        {
            if (now >= ring.EndTime)
            {
                RemCompDeferred<StoriesHereticFireRingComponent>(uid);
                continue;
            }

            if (now < ring.NextPulse)
                continue;

            ring.NextPulse = now + ring.Interval;
            _mobs.Clear();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, ring.Radius, _mobs);
            foreach (var mob in _mobs)
            {
                if (mob.Owner != uid)
                    Ignite(mob, ring.FireStacks);
            }
        }

        var masks = EntityQueryEnumerator<StoriesHereticMadnessMaskComponent>();
        while (masks.MoveNext(out _, out var mask))
        {
            if (mask.Wearer is not { } wearer || now < mask.NextPulse || !HasComp<StoriesHereticComponent>(wearer))
                continue;

            mask.NextPulse = now + mask.Interval;
            _mobs.Clear();
            _lookup.GetEntitiesInRange(Transform(wearer).Coordinates, mask.Range, _mobs);
            foreach (var mob in _mobs)
            {
                if (HasComp<StoriesHereticComponent>(mob) || !_mobState.IsAlive(mob))
                    continue;

                _stamina.TakeStaminaDamage(mob, mask.StaminaDamage, source: wearer);
                _statusEffects.TryUpdateStatusEffectDuration(mob, HallucinationEffect, mask.HallucinationTime);
            }
        }
    }

    private void OnMaskEquipped(Entity<StoriesHereticMadnessMaskComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.MASK) == 0)
            return;

        ent.Comp.Wearer = args.EquipTarget;
        ent.Comp.NextPulse = _timing.CurTime + ent.Comp.Interval;
    }

    private void OnMaskUnequipped(Entity<StoriesHereticMadnessMaskComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;
    }

    private void OnRitualEffect(ref StoriesHereticRitualEffectEvent args)
    {
        if (args.Effect != StoriesHereticRitualEffect.Ascension
            || args.Heretic.Comp.Path != AshPath
            || !ProtoMan.TryIndex(args.Ritual, out var proto)
            || !proto.TryComp<StoriesHereticAshAscensionRitualComponent>(out var ritual, _factory))
            return;

        var corpses = new List<EntityUid>();
        foreach (var uid in args.Nearby)
        {
            if (corpses.Count >= ritual.Corpses)
                break;

            if (HasComp<HumanoidProfileComponent>(uid) && _mobState.IsDead(uid) && IsBurnt(uid, ritual.HeatThreshold))
                corpses.Add(uid);
        }

        if (corpses.Count < ritual.Corpses)
        {
            args.FailReason = Loc.GetString("stories-heretic-ash-ascension-corpses", ("count", ritual.Corpses - corpses.Count));
            return;
        }

        args.Consumed.AddRange(corpses);
        Ascend(args.Heretic, ritual);
        args.Success = true;
    }

    private bool IsBurnt(EntityUid uid, FixedPoint2 threshold)
    {
        if (IsBurning(uid))
            return true;

        return TryComp<DamageableComponent>(uid, out var damageable)
               && damageable.Damage.DamageDict.TryGetValue("Heat", out var heat)
               && heat >= threshold;
    }

    private void Ascend(Entity<StoriesHereticComponent> heretic, StoriesHereticAshAscensionRitualComponent ritual)
    {
        _heretic.SetAscended(heretic);

        foreach (var action in ritual.Actions)
        {
            _actions.AddAction(heretic, action);
        }

        _damageable.SetDamageModifierSetId(heretic.Owner, ritual.DamageModifierSet);
        _statusEffects.TrySetStatusEffectDuration(heretic, PressureImmunityEffect);

        _chat.DispatchGlobalAnnouncement(Loc.GetString(ritual.Announcement, ("name", Name(heretic))),
            colorOverride: ritual.AnnouncementColor);
        _audio.PlayGlobal(ritual.Sound, Filter.Broadcast(), true);
    }

    private void OnFireRing(Entity<StoriesHereticComponent> ent, ref StoriesHereticFireRingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var ring = EnsureComp<StoriesHereticFireRingComponent>(ent);
        ring.EndTime = _timing.CurTime + args.Duration;
        ring.NextPulse = _timing.CurTime;
    }

    private void OnFireWave(Entity<StoriesHereticComponent> ent, ref StoriesHereticFireWaveEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var origin = _transform.GetMapCoordinates(ent).Position;
        var facing = _transform.GetWorldRotation(ent).ToWorldVec();
        var minDot = MathF.Cos((float) Angle.FromDegrees(args.HalfAngleDegrees).Theta);

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, args.Range, _mobs);
        foreach (var mob in _mobs)
        {
            if (mob.Owner == ent.Owner)
                continue;

            var direction = _transform.GetMapCoordinates(mob).Position - origin;
            if (direction.LengthSquared() < 0.01f || Vector2.Dot(Vector2.Normalize(direction), facing) < minDot)
                continue;

            _damageable.TryChangeDamage(mob.Owner, args.Damage, origin: ent);
            Ignite(mob, args.FireStacks);
        }
    }
}
```

В `StoriesHereticAshSystem.cs`, в конец `Initialize()`, добавить строку:
```csharp
        InitializeAscension();
```

- [ ] **Step 3: YAML**

Добавить в `ash_misc.yml`:
```yaml
- type: damageModifierSet
  id: STHereticAshAscended
  coefficients:
    Heat: 0
    Cold: 0
```

Добавить в `actions_ash.yml`:
```yaml
- type: entity
  id: STActionHereticFireRing
  name: Ring of Fire
  description: Surround yourself with fire for a minute.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    useDelay: 90
  - type: Sprite
    sprite: Objects/Magic/magicactions.rsi
    layers:
    - state: fireball
  - type: InstantAction
    event: !type:StoriesHereticFireRingEvent

- type: entity
  id: STActionHereticFireWave
  name: Wave of Flame
  description: Unleash a wave of fire in front of you.
  categories: [ HideSpawnMenu ]
  components:
  - type: Action
    itemIconStyle: NoItem
    useDelay: 30
  - type: Sprite
    sprite: Objects/Magic/magicactions.rsi
    layers:
    - state: fireball
  - type: InstantAction
    event: !type:StoriesHereticFireWaveEvent
      damage:
        types:
          Heat: 20
```

Добавить в `entities_ash.yml`:
```yaml
- type: entity
  parent: ClothingEyesBase
  id: STHereticAshenEyes
  name: ashen eyes
  description: A smouldering band worn over the eyes. Through it you see the warmth of the living.
  components:
  - type: Sprite
    sprite: Clothing/Eyes/Glasses/thermal.rsi
    color: "#d9792b"
  - type: Clothing
    sprite: Clothing/Eyes/Glasses/thermal.rsi
  - type: StoriesHereticFocus
  - type: VisionProvider
    isActive: true
    thermalVision: true
    ambientColor: "#FFFFFF10"
    priority: 5

- type: entity
  parent: ClothingMaskGas
  id: STHereticMadnessMask
  name: mask of madness
  description: A grinning mask. Those who see it lose their minds.
  components:
  - type: StoriesHereticMadnessMask
  - type: Sprite
    sprite: Clothing/Mask/goldenmask.rsi
    state: icon-despair
  - type: Clothing
    sprite: Clothing/Mask/goldenmask.rsi
```

Добавить в `rituals_ash.yml`:
```yaml
- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualAshenEyes
  name: Ashen Eyes
  description: A glass shard, a candle and a pair of eyes.
  components:
  - type: StoriesHereticRitual
    ingredients:
    - name: stories-heretic-ingredient-glass-shard
      whitelist:
        tags: [ GlassShard ]
    - name: stories-heretic-ingredient-candle
      whitelist:
        tags: [ Candle ]
    - name: stories-heretic-ingredient-eyes
      organCategory: Eyes
    results:
    - STHereticAshenEyes

- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualMadnessMask
  name: Mask of Madness
  description: A breath, gas or sterile mask, four candles, a stun baton and a liver.
  components:
  - type: StoriesHereticRitual
    ingredients:
    - name: stories-heretic-ingredient-mask
      prototypes: [ ClothingMaskGas, ClothingMaskBreath, ClothingMaskSterile ]
    - name: stories-heretic-ingredient-candle
      amount: 4
      whitelist:
        tags: [ Candle ]
    - name: stories-heretic-ingredient-stunbaton
      prototypes: [ Stunbaton ]
    - name: stories-heretic-ingredient-liver
      organCategory: Liver
    results:
    - STHereticMadnessMask

- type: entity
  parent: STHereticRitualBase
  id: STHereticRitualAshAscension
  name: Ashlord's Rite
  description: Three corpses that are burning or badly burnt.
  components:
  - type: StoriesHereticRitual
    effect: Ascension
  - type: StoriesHereticAshAscensionRitual
    actions:
    - STActionHereticFireRing
    - STActionHereticFireWave
```

Добавить в `knowledge_ash.yml`:
```yaml
- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshenEyes
  name: Ashen Eyes
  description: Forge a band that shows you the warmth of the living. It also works as a focus.
  components:
  - type: StoriesHereticKnowledge
    stage: 3
    requires: [ STHereticKnowledgeAshPassage ]
    rituals:
    - STHereticRitualAshenEyes

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeMadnessMask
  name: Mask of Madness
  description: Those around the wearer lose stamina and see things that are not there.
  components:
  - type: StoriesHereticKnowledge
    stage: 5
    requires: [ STHereticKnowledgeAshVolcano ]
    rituals:
    - STHereticRitualMadnessMask

- type: entity
  parent: STHereticKnowledgeBase
  id: STHereticKnowledgeAshAscension
  name: Ashlord's Rite
  description: Burn three corpses on a rune and ascend. Requires completed objectives.
  components:
  - type: StoriesHereticKnowledge
    path: STHereticPathAsh
    stage: 8
    cost: 2
    ascension: true
    requires: [ STHereticKnowledgeAshRebirth ]
    rituals:
    - STHereticRitualAshAscension
```

- [ ] **Step 4: Локализация**

ru-RU:
```ftl
stories-heretic-ash-ascension-corpses = Пламени нужно ещё { $count } сгоревших тел.
stories-heretic-ash-ascension-announcement = Внимание! Обнаружен всплеск аномальной активности. { $name } вознёсся как Повелитель пепла. Огонь поглотит всех.
stories-heretic-ingredient-glass-shard = осколок стекла
stories-heretic-ingredient-candle = свеча
stories-heretic-ingredient-mask = дыхательная, противогазная или стерильная маска
stories-heretic-ingredient-stunbaton = станбатон
stories-heretic-ingredient-liver = печень
```
en-US:
```ftl
stories-heretic-ash-ascension-corpses = The flame needs { $count } more burnt bodies.
stories-heretic-ash-ascension-announcement = Attention! A surge of anomalous activity detected. { $name } has ascended as the Ashlord. Fire will consume everyone.
stories-heretic-ingredient-glass-shard = glass shard
stories-heretic-ingredient-candle = candle
stories-heretic-ingredient-mask = breath, gas or sterile mask
stories-heretic-ingredient-stunbaton = stun baton
stories-heretic-ingredient-liver = liver
```

- [ ] **Step 5: Сборка, тесты, линтер, ручная проверка**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: `dotnet test ... --filter "FullyQualifiedName~Heretic"` → PASS.
Run: YAML-линтер → без ошибок.

Ручная проверка Вознесения (локальный сервер, см. Task 8, Step 7):
1. Через VV на `StoriesHereticComponent` выставить `Points = 30`, `LearnedCount = 6`, `SacrificeCount = 4`, `HeadSacrificeCount = 1`.
2. Изучить путь Ash до «Обряда Повелителя пепла».
3. Заспавнить 3 `MobHuman`, убить их уроном `Heat` (админ-смайт или `damage`-команда), положить на руну, активировать.
4. Ожидается: трупы исчезли, глобальное объявление, звук, появились «Кольцо огня» и «Волна пламени»; урон огнём по еретику не проходит.

- [ ] **Step 6: Коммит**

```bash
git add Content.Server/_Stories/Heretic Resources/Prototypes/_Stories/Heretic Resources/Locale
git commit -m "feat(heretic): Ashen Eyes, Mask of Madness, Ash ascension"
```

---

### Task 11: Гайдбук, русские имена сущностей, финальная проверка

**Files:**
- Create: `Resources/Prototypes/_Stories/Heretic/guidebook.yml`
- Create: `Resources/ServerInfo/_Stories/Heretic.xml`
- Create: `Resources/Locale/ru-RU/ss14-ru/prototypes/_stories/heretic/entities.ftl`
- Modify: `Resources/Prototypes/Guidebook/antagonist.yml` (апстрим, с пометкой)
- Modify: `Resources/Locale/{ru-RU,en-US}/_stories/heretic.ftl`

- [ ] **Step 1: Гайдбук**

`Resources/Prototypes/_Stories/Heretic/guidebook.yml`:
```yaml
- type: guideEntry
  id: STHeretic
  name: stories-guide-entry-heretic
  text: "/ServerInfo/_Stories/Heretic.xml"
```

`Resources/ServerInfo/_Stories/Heretic.xml`:
```xml
<Document>
  # Еретик

  Еретик слышит шёпот Мансуса — мира за гранью реальности. Он собирает запретные знания, приносит жертвы и стремится вознестись.

  ## Очки знаний
  - Влияния: разломы реальности, которые видит только еретик. Кликните пустой рукой, чтобы собрать (1 очко, с раскрытым Кодексом — 2).
  - Жертвы: 2 очка, глава отдела — 3.
  - Каждые 20 минут — 1 очко.
  - Ритуал знаний (один раз) — 4 очка. Пир сов — 5 очков, но Вознесение закрывается навсегда.

  ## Руны и ритуалы
  Возьмите ручку в активную руку и используйте действие «Начертить руну». Положите предметы на руну и кликните по ней пустой рукой. Хватка Мансуса стирает руны.

  ## Жертвоприношение
  Живое сердце указывает направление к целям. Положите цель — в критическом состоянии, мёртвую, связанную или спящую — на руну и активируйте её. Жертва исцеляется, на минуту попадает в Теневой мир и возвращается, не помня, кто это сделал.

  ## Фокус
  Сильные заклинания требуют фокуса: Янтарного фокуса на шее, Пепельных глаз или раскрытого Кодекса в руке.

  ## Путь Пепла
  Огонь и рывки. Метка Пепла поджигает жертву и распространяется. Вознесение: три сгоревших тела на руне.

  ## Как противостоять
  Капеллан и святые предметы в руках защищают от Хватки Мансуса. Разорванная реальность опасна — не смотрите на неё и не трогайте.
</Document>
```

В `Resources/Prototypes/Guidebook/antagonist.yml` в `children:` записи антагонистов добавить строку рядом с `STPontific`:
```yaml
  - STHeretic # Stories-Heretic
```

- [ ] **Step 2: Русские имена сущностей**

`Resources/Locale/ru-RU/ss14-ru/prototypes/_stories/heretic/entities.ftl`:
```ftl
ent-STHereticRune = руна трансмутации
    .desc = Аккуратно начерченный круг потусторонних символов.
ent-STHereticInfluence = разлом реальности
    .desc = Прореха в ткани мироздания. С той стороны что-то шепчет.
ent-STHereticPiercedReality = разорванная реальность
    .desc = Рана в мире. На неё больно смотреть.
ent-STHereticAmberFocus = янтарный фокус
    .desc = Стеклянный амулет с застывшим внутри глазом. Помогает сосредоточиться на Мансусе.
ent-STHereticCodexCicatrix = Кодекс Цикатрикс
    .desc = Книга в переплёте из кожи. Её страницы шепчут.
ent-STHereticBladeAsh = пепельный клинок
    .desc = Изогнутый клинок, пахнущий дымом. Сломайте его, и Мансус унесёт вас прочь.
ent-STHereticAshenEyes = пепельные глаза
    .desc = Тлеющая повязка на глаза. Сквозь неё видно тепло живых.
ent-STHereticMadnessMask = маска безумия
    .desc = Ухмыляющаяся маска. Те, кто её видит, теряют рассудок.
ent-STActionHereticKnowledge = Знания Мансуса
    .desc = Открыть древо запретных знаний.
ent-STActionHereticDrawRune = Начертить руну трансмутации
    .desc = Начертите руну пишущим предметом в активной руке.
ent-STActionHereticGrasp = Хватка Мансуса
    .desc = Коснитесь существа, чтобы оглушить его силой Мансуса. Также стирает руны.
ent-STActionHereticLivingHeart = Живое сердце
    .desc = Прислушайтесь к сердцу, чтобы найти следующую цель. Окружающие слышат его стук.
ent-STActionHereticAshPassage = Пепельный проход
    .desc = На миг рассыпьтесь пеплом и пройдите сквозь стены. Работает даже в наручниках.
ent-STActionHereticVolcanoBlast = Извержение вулкана
    .desc = Огненный луч, перескакивающий между ещё не горящими существами.
ent-STActionHereticRebirth = Перерождение Ночного дозорного
    .desc = Вытяните жизнь из всех горящих вокруг. Горящие в критическом состоянии умирают.
ent-STActionHereticFireRing = Кольцо огня
    .desc = Окружите себя огнём на минуту.
ent-STActionHereticFireWave = Волна пламени
    .desc = Обрушьте перед собой волну огня.
ent-STHereticKnowledgeRunes = Трансмутация Мансуса
    .desc = Вы умеете чертить руны трансмутации и проводить простые ритуалы.
ent-STHereticKnowledgeAmberFocus = Янтарный фокус
    .desc = Вы умеете создавать фокус из стекла и глаз.
ent-STHereticKnowledgeCodex = Кодекс Цикатрикс
    .desc = Вы умеете переплести Кодекс. Раскрытый Кодекс удваивает влияния, ускоряет руны и служит фокусом.
ent-STHereticKnowledgeBreakOfDawn = Рассвет
    .desc = Хватка Мансуса оглушает, сбивает с ног и путает речь жертвы.
ent-STHereticKnowledgeHeartbeat = Сердцебиение Мансуса
    .desc = Вы можете приносить цели в жертву на руне трансмутации.
ent-STHereticKnowledgeLivingHeart = Живое сердце
    .desc = Ваше сердце ведёт вас к целям.
ent-STHereticKnowledgeAshRoot = Секрет Ночного дозорного
    .desc = Ступите на Путь Пепла. Научитесь ковать пепельные клинки из ножа и огня.
ent-STHereticKnowledgeAshGrasp = Хватка Пепла
    .desc = Ваша Хватка Мансуса ослепляет жертву.
ent-STHereticKnowledgeAshPassage = Пепельный проход
    .desc = Короткий рывок сквозь стены. Работает в наручниках.
ent-STHereticKnowledgeAshenEyes = Пепельные глаза
    .desc = Создайте повязку, которая показывает тепло живых. Служит фокусом.
ent-STHereticKnowledgeAshMark = Метка Пепла
    .desc = Хватка метит жертв. Удар клинком по метке поджигает жертву, и метка перекидывается на соседей.
ent-STHereticKnowledgeAshVolcano = Извержение вулкана
    .desc = Огненный луч, перескакивающий между существами.
ent-STHereticKnowledgeMadnessMask = Маска безумия
    .desc = Окружающие владельца маски теряют выносливость и видят то, чего нет.
ent-STHereticKnowledgeAshFieryBlade = Огненный клинок
    .desc = Ваши клинки поджигают жертв.
ent-STHereticKnowledgeAshRebirth = Перерождение Ночного дозорного
    .desc = Вытяните жизнь из всех горящих вокруг.
ent-STHereticKnowledgeAshAscension = Обряд Повелителя пепла
    .desc = Сожгите три тела на руне и вознеситесь. Требует выполненных целей.
ent-STHereticRitualKnowledge = Ритуал знаний
    .desc = Соберите четыре предмета, которых требует Мансус. Можно провести один раз.
ent-STHereticRitualFeastOfOwls = Пир сов
    .desc = Получите много знаний разом, но навсегда лишитесь Вознесения.
ent-STHereticRitualAmberFocus = Янтарный фокус
    .desc = Лист стекла и пара глаз.
ent-STHereticRitualCodex = Кодекс Цикатрикс
    .desc = Книга, пишущий предмет и шкура или кожа.
ent-STHereticRitualSacrifice = Сердцебиение Мансуса
    .desc = Положите на руну цель в беспомощном состоянии.
ent-STHereticRitualReroll = Неумолимое сердцебиение
    .desc = Мак, книга и любая одежда. Даёт новые цели.
ent-STHereticRitualAshBlade = Пепельный клинок
    .desc = Нож и спичка или зажигалка.
ent-STHereticRitualAshenEyes = Пепельные глаза
    .desc = Осколок стекла, свеча и пара глаз.
ent-STHereticRitualMadnessMask = Маска безумия
    .desc = Маска, четыре свечи, станбатон и печень.
ent-STHereticRitualAshAscension = Обряд Повелителя пепла
    .desc = Три горящих или сильно обожжённых тела.
```

Добавить в ru-RU `heretic.ftl`:
```ftl
stories-guide-entry-heretic = Еретик
```
Добавить в en-US `heretic.ftl`:
```ftl
stories-guide-entry-heretic = Heretic
```

- [ ] **Step 3: Полная проверка**

Run: `dotnet build -c Debug` → `0 Error(s)`, без новых предупреждений в файлах `_Stories/Heretic` (сравнить с выводом сборки на `master`).
Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~Heretic"` → все тесты PASS.
Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~EntityTest|FullyQualifiedName~AllGamePresetsStartTest|FullyQualifiedName~SecretStartsTest"` → PASS (спавн всех сущностей, запуск всех режимов, Secret).
Run: YAML-линтер → без ошибок.

- [ ] **Step 4: Коммит**

```bash
git add Resources/Prototypes/_Stories/Heretic Resources/Prototypes/Guidebook/antagonist.yml Resources/ServerInfo/_Stories/Heretic.xml Resources/Locale
git commit -m "feat(heretic): guidebook and Russian entity names"
```

---

### Task 12: Спрайты из tgstation

Спрайты Еретика берутся из tgstation (лицензия CC-BY-SA 3.0 — разрешена разделом «License Compatibility», п. 2 лицензии Space Stories). Коммит tgstation зафиксирован: `ca79910d79497cf1ab56ebcf8db52a7289a3d412`.

**Files:**
- Create: `Tools/_Stories/dmi_to_rsi.py` (конвертер DMI → RSI, пригодится для следующих путей)
- Create: `Resources/Textures/_Stories/Heretic/*.rsi` (см. таблицу)
- Modify: `Content.Shared/_Stories/Vision/Events/VisionEvents.cs` (зрение из слота шеи — для медальона Пепельных глаз, как в tg)
- Modify: все YAML Еретика, где стоят заглушки-спрайты
- Modify: `docs/superpowers/specs/2026-10-13-heretic-core-design.md` (Пепельные глаза снова медальон)

**Interfaces:**
- Consumes: прототипы из задач 1–10.
- Produces: RSI в `Resources/Textures/_Stories/Heretic/`.

- [ ] **Step 1: Конвертер**

`Tools/_Stories/dmi_to_rsi.py`:
```python
#!/usr/bin/env python3
"""
Конвертирует состояния BYOND DMI в RSI (Robust Toolbox).

Использование:
  python Tools/_Stories/dmi_to_rsi.py <file.dmi> <out.rsi> "<copyright>" <dmi_state>[=<rsi_state>] ...

Если RSI уже существует, состояния добавляются или заменяются (размер кадра должен совпадать).
Требуется Pillow.
"""
import json
import math
import re
import sys
from pathlib import Path

from PIL import Image


def parse_description(desc):
    width = int(re.search(r"width = (\d+)", desc).group(1))
    height = int(re.search(r"height = (\d+)", desc).group(1))
    states = []
    current = None
    for raw in desc.splitlines():
        line = raw.strip()
        match = re.match(r'state = "(.*)"', line)
        if match:
            current = {"name": match.group(1), "dirs": 1, "frames": 1, "delay": None}
            states.append(current)
            continue
        if current is None:
            continue
        match = re.match(r"(dirs|frames) = (\d+)", line)
        if match:
            current[match.group(1)] = int(match.group(2))
            continue
        match = re.match(r"delay = (.*)", line)
        if match:
            current["delay"] = [float(x) for x in match.group(1).split(",")]
    return width, height, states


def main():
    if len(sys.argv) < 5:
        sys.exit(__doc__)

    dmi_path, out_path, copyright_text, *specs = sys.argv[1:]
    sheet = Image.open(dmi_path)
    width, height, states = parse_description(sheet.info["Description"])
    sheet = sheet.convert("RGBA")
    sheet_columns = sheet.width // width

    # В DMI кадры идут подряд по всем состояниям; внутри состояния — кадр за кадром, в кадре — все направления.
    # При повторяющихся именах берётся первое состояние.
    offsets = {}
    index = 0
    for state in states:
        offsets.setdefault(state["name"], (index, state))
        index += state["dirs"] * state["frames"]

    out_dir = Path(out_path)
    out_dir.mkdir(parents=True, exist_ok=True)
    meta_path = out_dir / "meta.json"
    if meta_path.exists():
        meta = json.loads(meta_path.read_text(encoding="utf-8"))
        if meta["size"] != {"x": width, "y": height}:
            sys.exit(f"{out_path}: size {meta['size']} != {width}x{height}")
    else:
        meta = {"version": 1, "license": "CC-BY-SA-3.0", "copyright": copyright_text,
                "size": {"x": width, "y": height}, "states": []}

    for spec in specs:
        source, _, target = spec.partition("=")
        target = target or source
        if source not in offsets:
            sys.exit(f"{dmi_path}: no state '{source}'")

        start, state = offsets[source]
        dirs, frames = state["dirs"], state["frames"]
        total = dirs * frames
        columns = math.ceil(math.sqrt(total))
        rows = math.ceil(total / columns)
        image = Image.new("RGBA", (columns * width, rows * height))

        # RSI: сначала все кадры направления 0, затем направления 1 и т.д.; сетка слева направо, сверху вниз.
        for direction in range(dirs):
            for frame in range(frames):
                source_index = start + frame * dirs + direction
                sx, sy = (source_index % sheet_columns) * width, (source_index // sheet_columns) * height
                tile = sheet.crop((sx, sy, sx + width, sy + height))
                target_index = direction * frames + frame
                image.paste(tile, ((target_index % columns) * width, (target_index // columns) * height))

        image.save(out_dir / f"{target}.png")

        entry = {"name": target}
        if dirs != 1:
            entry["directions"] = dirs
        if frames > 1:
            delay = state["delay"] or [1.0] * frames
            entry["delays"] = [[round(d / 10, 3) for d in delay] for _ in range(dirs)]

        meta["states"] = [s for s in meta["states"] if s["name"] != target] + [entry]

    meta_path.write_text(json.dumps(meta, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Скачать DMI и сконвертировать**

DMI — внешние файлы, поэтому качаем их во временную папку вне репозитория и передаём пути аргументами (Bash, из корня репозитория):
```bash
TG=ca79910d79497cf1ab56ebcf8db52a7289a3d412
DMI="$(mktemp -d)"
for p in icons/effects/eldritch.dmi icons/effects/96x96.dmi icons/obj/antags/eldritch.dmi icons/mob/actions/actions_ecult.dmi \
         icons/obj/weapons/khopesh.dmi icons/mob/inhands/64x64_lefthand.dmi icons/mob/inhands/64x64_righthand.dmi \
         icons/obj/clothing/neck.dmi icons/mob/clothing/neck.dmi icons/obj/clothing/masks.dmi icons/mob/clothing/mask.dmi; do
  curl -sfL -o "$DMI/$(echo $p | tr / _)" "https://raw.githubusercontent.com/tgstation/tgstation/$TG/$p"
done

C="Taken from tgstation at https://github.com/tgstation/tgstation/commit/$TG"
T=Resources/Textures/_Stories/Heretic
py() { python -P -E Tools/_Stories/dmi_to_rsi.py "$@"; }

py "$DMI/icons_effects_96x96.dmi"              $T/rune.rsi          "$C" transmutation_rune
py "$DMI/icons_effects_eldritch.dmi"           $T/effects.rsi       "$C" reality_smash pierced_illusion
py "$DMI/icons_obj_antags_eldritch.dmi"        $T/codex.rsi         "$C" book=icon
py "$DMI/icons_obj_clothing_neck.dmi"          $T/amber_focus.rsi   "$C" eldritch_necklace=icon
py "$DMI/icons_mob_clothing_neck.dmi"          $T/amber_focus.rsi   "$C" eldritch_necklace=equipped-NECK
py "$DMI/icons_obj_antags_eldritch.dmi"        $T/ashen_eyes.rsi    "$C" eye_medalion=icon
py "$DMI/icons_mob_clothing_neck.dmi"          $T/ashen_eyes.rsi    "$C" eye_medalion=equipped-NECK
py "$DMI/icons_obj_clothing_masks.dmi"         $T/madness_mask.rsi  "$C" mad_mask=icon
py "$DMI/icons_mob_clothing_mask.dmi"          $T/madness_mask.rsi  "$C" mad_mask=equipped-MASK
py "$DMI/icons_obj_weapons_khopesh.dmi"        $T/ash_blade.rsi     "$C" ash_blade=icon
py "$DMI/icons_mob_inhands_64x64_lefthand.dmi" $T/ash_blade_inhand.rsi "$C" ash_blade=inhand-left
py "$DMI/icons_mob_inhands_64x64_righthand.dmi" $T/ash_blade_inhand.rsi "$C" ash_blade=inhand-right
py "$DMI/icons_mob_actions_actions_ecult.dmi"  $T/actions.rsi       "$C" eye mansus_grasp ash_shift flames smoke fire_ring
py "$DMI/icons_effects_eldritch.dmi"           $T/actions.rsi       "$C" small_rune_1=draw_rune
py "$DMI/icons_obj_antags_eldritch.dmi"        $T/actions.rsi       "$C" living_heart
rm -rf "$DMI"
```
Expected: команды завершаются без вывода; в `Resources/Textures/_Stories/Heretic/` 9 папок `.rsi`, в каждой `meta.json` и PNG.

- [ ] **Step 3: Зрение из слота шеи (медальон Пепельных глаз, как в tg)**

`Content.Shared/_Stories/Vision/Events/VisionEvents.cs`, свойство `TargetSlots` в `RefreshVisionEvent`:
```csharp
    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.OUTERCLOTHING | SlotFlags.NECK;
```
Это файл форка, пометки `// Stories-...` не нужны.

- [ ] **Step 4: Заменить заглушки в YAML**

`entities.yml`:
- `STHereticRune` → `Sprite`: `sprite: _Stories/Heretic/rune.rsi`, `state: transmutation_rune`, `color: "#4bd36a"`, `drawdepth: FloorObjects`; строку `scale` удалить (спрайт 96×96 уже покрывает 3×3).
- `STHereticInfluence` → `sprite: _Stories/Heretic/effects.rsi`, `state: reality_smash`, строку `color` удалить, `visible: false` оставить.
- `STHereticPiercedReality` → `sprite: _Stories/Heretic/effects.rsi`, `state: pierced_illusion`, строку `color` удалить.
- `STHereticAmberFocus` → `Sprite: sprite: _Stories/Heretic/amber_focus.rsi`, `state: icon` (без `color`); `Clothing: sprite: _Stories/Heretic/amber_focus.rsi`.
- `STHereticCodexCicatrix` → `sprite: _Stories/Heretic/codex.rsi`, `state: icon`, без `color`.

`entities_ash.yml`:
- `STHereticBladeAsh` → `Sprite: sprite: _Stories/Heretic/ash_blade.rsi`, `state: icon`, без `color`; `Item: sprite: _Stories/Heretic/ash_blade_inhand.rsi`.
- `STHereticAshenEyes` → заменить целиком:
```yaml
- type: entity
  parent: ClothingNeckBase
  id: STHereticAshenEyes
  name: warm eldritch medallion
  description: A smouldering medallion with an eye. Through it you see the warmth of the living.
  components:
  - type: Sprite
    sprite: _Stories/Heretic/ashen_eyes.rsi
    state: icon
  - type: Clothing
    sprite: _Stories/Heretic/ashen_eyes.rsi
  - type: StoriesHereticFocus
  - type: VisionProvider
    isActive: true
    thermalVision: true
    ambientColor: "#FFFFFF10"
    priority: 5
```
- `STHereticMadnessMask` → `Sprite: sprite: _Stories/Heretic/madness_mask.rsi`, `state: icon`; `Clothing: sprite: _Stories/Heretic/madness_mask.rsi`.

Действия (`actions.yml`, `actions_ash.yml`) — в каждом `Sprite` поставить `sprite: _Stories/Heretic/actions.rsi` и состояние:

| Действие | state |
|---|---|
| `STActionHereticKnowledge` | `eye` |
| `STActionHereticDrawRune` | `draw_rune` |
| `STActionHereticGrasp` | `mansus_grasp` |
| `STActionHereticLivingHeart` | `living_heart` |
| `STActionHereticAshPassage` | `ash_shift` |
| `STActionHereticVolcanoBlast` | `flames` |
| `STActionHereticRebirth` | `smoke` |
| `STActionHereticFireRing` | `fire_ring` |
| `STActionHereticFireWave` | `flames` |

`objectives.yml` → иконка `STBaseHereticObjective`: `sprite: _Stories/Heretic/actions.rsi`, `state: eye`.

Русские имена: в `Resources/Locale/ru-RU/ss14-ru/prototypes/_stories/heretic/entities.ftl` заменить строку `ent-STHereticAshenEyes`:
```ftl
ent-STHereticAshenEyes = тёплый потусторонний медальон
    .desc = Тлеющий медальон с глазом. Сквозь него видно тепло живых.
```
В `Resources/ServerInfo/_Stories/Heretic.xml` заменить «Пепельных глаз» на «тёплого медальона».

- [ ] **Step 5: Спецификация**

В `docs/superpowers/specs/2026-10-13-heretic-core-design.md`, раздел 6, строка «Пепельные глаза»: «→ медальон на шею с тепловым зрением (как в tg; зрение форка научено работать из слота шеи). Служит фокусом». Раздел 10, пункт «Текстуры»: «Спрайты взяты из tgstation (CC-BY-SA 3.0, коммит `ca79910d`), конвертер — `Tools/_Stories/dmi_to_rsi.py`».

- [ ] **Step 6: Проверка**

Run: `dotnet build -c Debug` → `0 Error(s)`.
Run: YAML-линтер → без ошибок (он проверяет, что RSI и состояния существуют).
Run: `dotnet test ... --filter "FullyQualifiedName~Heretic|FullyQualifiedName~EntityTest"` → PASS.
Ручная проверка (Task 8, Step 7): руна зелёная 3×3, влияния анимированы, клинок виден в руке, медальон и маска видны на персонаже, у действий иконки tg.

- [ ] **Step 7: Коммит**

```bash
git add Tools/_Stories/dmi_to_rsi.py Resources/Textures/_Stories/Heretic Content.Shared/_Stories/Vision/Events/VisionEvents.cs Resources/Prototypes/_Stories/Heretic Resources/Locale Resources/ServerInfo/_Stories/Heretic.xml docs/superpowers/specs
git commit -m "feat(heretic): sprites from tgstation (CC-BY-SA 3.0)"
```
