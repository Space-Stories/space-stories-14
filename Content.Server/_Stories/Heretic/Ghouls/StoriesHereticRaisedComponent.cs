namespace Content.Server._Stories.Heretic.Ghouls;

/// <summary>
/// Труп уже служил гулем — поднять его второй раз нельзя (Paradise: «once per corpse»).
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticRaisedComponent : Component;
