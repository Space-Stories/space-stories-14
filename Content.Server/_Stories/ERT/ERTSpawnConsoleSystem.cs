using Content.Server.GameTicking;
using Content.Shared._Stories.ERT;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Stories.ERT;

public sealed partial class ERTSpawnConsoleSystem : EntitySystem
{
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;

    private Dictionary<EntityUid, int> _activeCooldowns = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ERTSpawnConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ERTSpawnConsoleComponent, ERTSpawnConsoleCallMessage>(OnCallMessage);
    }

    private void OnUiOpened(EntityUid uid, ERTSpawnConsoleComponent component, BoundUIOpenedEvent args)
    {
        bool onCooldown = _activeCooldowns.ContainsKey(uid);
        int activeIndex = onCooldown ? _activeCooldowns[uid] : -1;
        var availability = GetAvailability(component);

        var state = new ERTSpawnConsoleState(
            component.Options,
            onCooldown,
            activeIndex,
            component.Balance,
            component.AdminAccess,
            availability,
            component.TitleName,
            component.BalanceName);
        _uiSystem.SetUiState(uid, ERTSpawnConsoleUiKey.Key, state);
    }

    private void OnCallMessage(EntityUid uid, ERTSpawnConsoleComponent component, ERTSpawnConsoleCallMessage args)
    {
        if (args.OptionIndex < 0 || args.OptionIndex >= component.Options.Count)
            return;
        if (_activeCooldowns.ContainsKey(uid))
            return;

        var option = component.Options[args.OptionIndex];

        if (option.RequiresAdminAllow && !component.AdminAccess)
            return;
        if (option.Price > component.Balance)
            return;

        _gameTicker.AddGameRule(option.Rule);
        component.Balance -= option.Price;

        _activeCooldowns[uid] = args.OptionIndex;

        var availabilityDuringCooldown = GetAvailability(component);
        var cooldownState = new ERTSpawnConsoleState(
            component.Options,
            true,
            args.OptionIndex,
            component.Balance,
            component.AdminAccess,
            availabilityDuringCooldown,
            component.TitleName,
            component.BalanceName);
        _uiSystem.SetUiState(uid, ERTSpawnConsoleUiKey.Key, cooldownState);

        Timer.Spawn(TimeSpan.FromSeconds(component.Delay), () =>
        {
            if (!_activeCooldowns.ContainsKey(uid))
                return;

            _activeCooldowns.Remove(uid);

            // После снятия кулдауна отправляем обновлённое состояние (если консоль ещё существует)
            if (!TryComp<ERTSpawnConsoleComponent>(uid, out var comp))
                return;

            var normalAvailability = GetAvailability(comp);
            var normalState = new ERTSpawnConsoleState(
                comp.Options,
                false,
                -1,
                comp.Balance,
                comp.AdminAccess,
                normalAvailability,
                component.TitleName,
                component.BalanceName);
            _uiSystem.SetUiState(uid, ERTSpawnConsoleUiKey.Key, normalState);
        });
    }

    private List<bool> GetAvailability(ERTSpawnConsoleComponent component)
    {
        var result = new List<bool>(component.Options.Count);

        foreach (var option in component.Options)
        {
            if (option.RequiresAdminAllow && !component.AdminAccess)
            {
                result.Add(false);
                continue;
            }

            if (option.Price > component.Balance)
            {
                result.Add(false);
                continue;
            }

            result.Add(true);
        }

        return result;
    }
}
