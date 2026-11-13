using Content.Server.Silicons.Laws;
using Content.Server.Preferences.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared._Stories.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Server._Stories.Silicons;

public sealed partial class LawsetLoadoutSystem : EntitySystem
{
    [Dependency] private SiliconLawSystem _lawSystem = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete, after: new[] { typeof(SiliconLawSystem) });
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId != "StationAi" || args.Player == null)
            return;

        HumanoidCharacterProfile? profile = args.Profile;

        if (profile?.Loadouts == null || profile.Loadouts.Count == 0)
        {
            var savedPrefs = _prefs.GetPreferences(args.Player.UserId);
            if (savedPrefs?.SelectedCharacter is HumanoidCharacterProfile savedProfile)
            {
                profile = savedProfile;
            }
            else
            {
                return;
            }
        }

        if (profile.Loadouts == null || !TryComp<SiliconLawBoundComponent>(args.Mob, out _))
            return;

        LoadoutPrototype? loadoutProto = null;
        bool foundInProfile = false;

        foreach (var roleLoadout in profile.Loadouts.Values)
        {
            if (roleLoadout.SelectedLoadouts.TryGetValue("STLawset", out var existingList) && existingList.Count > 0)
            {
                var selectedLoadout = existingList[0];
                if (_protoMan.TryIndex(selectedLoadout.Prototype, out loadoutProto))
                {
                    foundInProfile = true;
                    break;
                }
            }
        }

        if (!foundInProfile)
        {
            var groupId = new ProtoId<LoadoutGroupPrototype>("STLawset");
            if (_protoMan.TryIndex(groupId, out var groupProto) && groupProto.Loadouts.Count > 0)
            {
                var fallbackId = groupProto.Loadouts[0];
                if (!_protoMan.TryIndex(fallbackId, out loadoutProto))
                    return;
            }
            else
            {
                return;
            }
        }

        if (loadoutProto == null)
            return;

        SiliconLawsetLoadoutEffect? lawEffect = null;
        foreach (var effect in loadoutProto.Effects)
        {
            if (effect is SiliconLawsetLoadoutEffect found)
            {
                lawEffect = found;
                break;
            }
        }

        if (lawEffect == null || !_protoMan.TryIndex(lawEffect.Lawset, out var lawsetProto) || lawsetProto == null)
            return;

        var newLaws = new List<SiliconLaw>();
        foreach (var lawId in lawsetProto.Laws)
        {
            if (_protoMan.TryIndex(lawId, out SiliconLawPrototype? lawProto) && lawProto != null)
            {
                newLaws.Add(new SiliconLaw
                {
                    Order = lawProto.Order,
                    LawString = lawProto.LawString
                });
            }
        }

        if (newLaws.Count == 0)
            return;

        _lawSystem.SetLaws(newLaws, args.Mob);
    }
}
