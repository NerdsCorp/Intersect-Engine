namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class RateHouseCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.RateHouse;

    /// <summary>
    /// The rating value (1-5).
    /// </summary>
    public int Rating { get; set; } = 5;

    /// <summary>
    /// Optional variable containing the rating value.
    /// </summary>
    public Guid RatingVariableId { get; set; } = Guid.Empty;
}
