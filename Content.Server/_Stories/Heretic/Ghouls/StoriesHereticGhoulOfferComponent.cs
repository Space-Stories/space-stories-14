using Content.Shared._Stories.Conversion;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Stories.Heretic.Ghouls;

/// <summary>
/// На трупе без души: ждёт игрока из призрачной роли, чтобы поднять труп гулем.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StoriesHereticGhoulSystem))]
public sealed partial class StoriesHereticGhoulOfferComponent : Component
{
    [DataField]
    public EntityUid Master;

    [DataField]
    public ProtoId<ConversionPrototype> Conversion;

    [DataField]
    public int Limit;

    [DataField]
    public EntProtoId Refund;

    /// <summary>
    /// Предложение закрыто штатно (взято, истекло, труп оживлён): возвращать мак при снятии компонента не нужно.
    /// </summary>
    [DataField]
    public bool Settled;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ExpireTime;
}
