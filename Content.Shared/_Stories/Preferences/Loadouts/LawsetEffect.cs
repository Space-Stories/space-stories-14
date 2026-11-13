using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Silicons.Laws;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Stories.Preferences.Loadouts;

public sealed partial class SiliconLawsetLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public ProtoId<SiliconLawsetPrototype> Lawset { get; private set; } = default!;

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        return true;
    }
}
