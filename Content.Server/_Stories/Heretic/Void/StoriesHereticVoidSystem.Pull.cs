using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Chat;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;

namespace Content.Server._Stories.Heretic.Void;

public sealed partial class StoriesHereticVoidSystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private readonly HashSet<Entity<MobStateComponent>> _pullMobs = new();

    private void InitializePull()
    {
        SubscribeLocalEvent<StoriesHereticComponent, StoriesHereticVoidPullEvent>(OnVoidPull);
    }

    private void OnVoidPull(Entity<StoriesHereticComponent> ent, ref StoriesHereticVoidPullEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _chat.TrySendInGameICMessage(ent, Loc.GetString(args.Speech), InGameICChatType.Whisper, false);

        var origin = Transform(ent).Coordinates;
        var originPos = _transform.ToMapCoordinates(origin).Position;

        _pullMobs.Clear();
        _lookup.GetEntitiesInRange(origin, args.PullRadius, _pullMobs);
        foreach (var mob in _pullMobs)
        {
            if (mob.Owner == ent.Owner
                || !_mobState.IsAlive(mob)
                || HasComp<StoriesHereticComponent>(mob)
                || _heretic.IsServant(mob))
                continue;

            var offset = originPos - _transform.GetWorldPosition(mob);
            var distance = offset.Length();

            if (distance <= args.KnockdownRadius)
            {
                if (distance <= args.DamageRadius)
                    _damageable.TryChangeDamage(mob.Owner, args.Damage, origin: ent);

                _stun.TryKnockdown(mob.Owner, args.Knockdown);
            }
            else
            {
                _throwing.TryThrow(mob, offset.Normalized() * MathF.Min(args.PullDistance, distance), compensateFriction: true, recoil: false, doSpin: false);
            }
        }

        Spawn(args.Effect, origin);
        _audio.PlayPvs(args.Sound, origin);
    }
}
