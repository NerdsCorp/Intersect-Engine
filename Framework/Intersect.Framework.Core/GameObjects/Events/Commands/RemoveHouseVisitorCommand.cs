namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class RemoveHouseVisitorCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.RemoveHouseVisitor;

    /// <summary>
    /// Variable containing the name of the player to remove.
    /// </summary>
    public Guid PlayerVariableId { get; set; }
}
