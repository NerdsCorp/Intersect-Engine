namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class OpenHouseFurnitureCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.OpenHouseFurniture;
}
