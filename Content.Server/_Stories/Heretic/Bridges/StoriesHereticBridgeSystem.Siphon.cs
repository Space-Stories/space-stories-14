using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Robust.Shared.Random;

namespace Content.Server._Stories.Heretic.Bridges;

public sealed partial class StoriesHereticBridgeSystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IRobustRandom _random = default!;

    private void InitializeSiphon()
    {
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticBloodSiphonEvent>(OnBloodSiphon);
    }

    private void OnBloodSiphon(Entity<StoriesHereticComponent> ent, ref StoriesHereticBloodSiphonEvent args)
    {
        if (args.Handled || args.Target == ent.Owner || !IsValidVictim(args.Target))
            return;

        args.Handled = true;

        _damageable.TryChangeDamage(args.Target, args.Damage, origin: ent);
        _damageable.HealDistributed(ent.Owner, -args.Heal, "Brute", ent);
        _bloodstream.TryModifyBloodLevel(ent.Owner, args.Blood);

        if (TryComp<BloodstreamComponent>(ent, out var bloodstream)
            && bloodstream.BleedAmount > 0
            && _random.Prob(args.BleedChance))
        {
            var moved = bloodstream.BleedAmount * args.BleedShare;
            _bloodstream.TryModifyBleedAmount(ent.Owner, -moved);
            _bloodstream.TryModifyBleedAmount(args.Target, moved);
        }

        _audio.PlayPvs(args.Sound, args.Target);
    }
}
