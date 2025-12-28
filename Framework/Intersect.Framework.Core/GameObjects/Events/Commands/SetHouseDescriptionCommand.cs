namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class SetHouseDescriptionCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.SetHouseDescription;

    /// <summary>
    /// Variable containing the house description to set.
    /// </summary>
    public Guid DescriptionVariableId { get; set; }
}
