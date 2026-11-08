using Content.Server.Humanoid.Systems;
using Content.Shared._Stories.ERT;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Stories.ERT;

public sealed partial class ERTCryostorageSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ERTCryostorageComponent>(ERTCryostorageUiKey.Key, subs =>
        {
            subs.Event<ERTCryostorageSpawnMessage>(OnSpawnMessage);
        });

        SubscribeLocalEvent<ERTCryostorageComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    private void OnUiOpened(EntityUid uid, ERTCryostorageComponent component, BoundUIOpenedEvent args)
    {
        var state = new ERTCryostorageState(
            component.Options,
            component.TitleName,
            component.AdminAccess,
            component.Blocked);

        _uiSystem.SetUiState(uid, ERTCryostorageUiKey.Key, state);
    }

    private void OnSpawnMessage(EntityUid uid, ERTCryostorageComponent component, ERTCryostorageSpawnMessage args)
    {
        if (args.OptionIndex < 0 || args.OptionIndex >= component.Options.Count)
            return;

        if (component.UseOnce && component.Blocked)
            return;

        var option = component.Options[args.OptionIndex];
        if (option.RequiresAdminAllow && !component.AdminAccess)
            return;

        if (!_container.TryGetContainer(uid, component.ContainerId, out var container))
            return;

        if (string.IsNullOrEmpty(option.Settings))
            return;

        var humanoid = _randomHumanoid.SpawnRandomHumanoid(
            option.Settings,
            Transform(uid).Coordinates,
            "ERT Operative");

        if (!_container.Insert(humanoid, container))
        {
            QueueDel(humanoid);
            return;
        }

        if (component.UseOnce)
            component.Blocked = true;

        if (_uiSystem.TryGetUiState<ERTCryostorageState>(uid, ERTCryostorageUiKey.Key, out _))
        {
            var newState = new ERTCryostorageState(
                component.Options,
                component.TitleName,
                component.AdminAccess,
                component.Blocked);
            _uiSystem.SetUiState(uid, ERTCryostorageUiKey.Key, newState);
        }
    }
}
