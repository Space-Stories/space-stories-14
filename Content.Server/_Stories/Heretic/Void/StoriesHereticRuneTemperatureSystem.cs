using Content.Server.Atmos.EntitySystems;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Atmos;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticRuneTemperatureSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoriesHereticRuneTemperatureEvent>(OnRuneTemperature);
    }

    private void OnRuneTemperature(ref StoriesHereticRuneTemperatureEvent args)
    {
        args.Temperature = _atmosphere.GetTileMixture(args.Rune)?.Temperature ?? Atmospherics.TCMB;
        args.Handled = true;
    }
}
