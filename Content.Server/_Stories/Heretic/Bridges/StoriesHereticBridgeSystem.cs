using Content.Shared._Stories.Heretic;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Stories.Heretic.Bridges;

public sealed partial class StoriesHereticBridgeSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StoriesHereticSystem _heretic = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeCleave();
        InitializePrison();
        InitializeSiphon();
    }

    private bool IsValidVictim(EntityUid target)
    {
        return HasComp<MobStateComponent>(target)
               && !_mobState.IsDead(target)
               && !HasComp<StoriesHereticComponent>(target)
               && !_heretic.IsServant(target);
    }
}
