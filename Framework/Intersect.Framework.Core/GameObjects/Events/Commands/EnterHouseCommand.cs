namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class EnterHouseCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.EnterHouse;

    /// <summary>
    /// Player ID whose house to enter. If Guid.Empty, enters own house.
    /// </summary>
    public Guid TargetPlayerId { get; set; } = Guid.Empty;

    /// <summary>
    /// Variable containing player name to look up. Takes priority over TargetPlayerId.
    /// </summary>
    public Guid PlayerVariableId { get; set; } = Guid.Empty;

    /// <summary>
    /// X coordinate to spawn at in the house.
    /// </summary>
    public byte X { get; set; } = 5;

    /// <summary>
    /// Y coordinate to spawn at in the house.
    /// </summary>
    public byte Y { get; set; } = 5;
}
