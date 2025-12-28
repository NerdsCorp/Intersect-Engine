namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class SetHouseNameCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.SetHouseName;

    /// <summary>
    /// Variable containing the house name to set.
    /// </summary>
    public Guid NameVariableId { get; set; }
}
