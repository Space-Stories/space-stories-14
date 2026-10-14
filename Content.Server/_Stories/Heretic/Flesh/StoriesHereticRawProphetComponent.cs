namespace Content.Server._Stories.Heretic.Flesh;

/// <summary>
/// На Сыром пророке: состояние Связи Мансуса и Расширения взора.
/// </summary>
[RegisterComponent, Access(typeof(StoriesHereticFleshSystem))]
public sealed partial class StoriesHereticRawProphetComponent : Component
{
    /// <summary>Хозяин-еретик: получает канал, когда игрок занимает пророка.</summary>
    [DataField]
    public EntityUid? Master;

    /// <summary>Связанные цели; при разрыве канал снимается со всех.</summary>
    [DataField]
    public HashSet<EntityUid> Linked = new();

    /// <summary>Паралич связанных при разрыве.</summary>
    [DataField]
    public TimeSpan BreakParalyze = TimeSpan.FromSeconds(0.5);

    /// <summary>Расширение взора сейчас включено.</summary>
    public bool SightExpanded;
}
