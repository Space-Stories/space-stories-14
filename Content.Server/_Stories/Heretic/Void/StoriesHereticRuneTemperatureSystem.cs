using Content.Server.Atmos.EntitySystems;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Atmos;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticRuneTemperatureSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    private static readonly float VacuumMoles = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRuneTemperatureEvent>(OnRuneTemperature);
    }

    private void OnRuneTemperature(ref StoriesHereticRuneTemperatureEvent args)
    {
        var mixture = _atmosphere.GetTileMixture(args.Rune);
        args.Temperature = mixture == null || mixture.TotalMoles < VacuumMoles
            ? Atmospherics.TCMB
            : mixture.Temperature;
        args.Handled = true;
    }
}
