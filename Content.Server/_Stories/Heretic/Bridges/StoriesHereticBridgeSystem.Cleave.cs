using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Mobs.Components;

namespace Content.Server._Stories.Heretic.Bridges;

public sealed partial class StoriesHereticBridgeSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;

    private readonly HashSet<Entity<MobStateComponent>> _cleaveMobs = new();

    private void InitializeCleave()
    {
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticBloodCleaveEvent>(OnBloodCleave);
    }

    private void OnBloodCleave(Entity<StoriesHereticComponent> ent, ref StoriesHereticBloodCleaveEvent args)
    {
        if (args.Handled || args.Target == ent.Owner || !IsValidVictim(args.Target))
            return;

        args.Handled = true;

        var coords = Transform(args.Target).Coordinates;
        _cleaveMobs.Clear();
        _lookup.GetEntitiesInRange(coords, args.Radius, _cleaveMobs);
        foreach (var mob in _cleaveMobs)
        {
            if (mob.Owner == ent.Owner || !IsValidVictim(mob))
                continue;

            _damageable.TryChangeDamage(mob.Owner, args.Damage, origin: ent);
            _bloodstream.TryModifyBleedAmount(mob.Owner, args.Bleed);
        }

        Spawn(args.Effect, coords);
        Spawn(args.Puddle, coords);
        _audio.PlayPvs(args.Sound, coords);
    }
}
