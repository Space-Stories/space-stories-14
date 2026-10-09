using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

// \Content.Shared\Preferences\Loadouts\LoadoutPrototype.cs
namespace Content.Shared.Preferences.Loadouts;

public sealed partial class LoadoutPrototype
{
    [DataField]
    public ProtoId<SiliconLawsetPrototype>? SiliconLawset;
}
