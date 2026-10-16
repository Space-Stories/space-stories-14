namespace Content.Server._Stories.Heretic;

/// <summary>
/// Точка появления жертвы на карте Теневого мира, расставленной маппером.
/// Если на загруженной карте таких точек нет, жертва появляется в центре сетки.
/// </summary>
[RegisterComponent]
public sealed partial class StoriesHereticShadowRealmSpawnComponent : Component;
