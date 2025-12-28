namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class OpenFurnitureStorageCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.OpenFurnitureStorage;

    /// <summary>
    /// The furniture slot index containing the storage to open.
    /// </summary>
    public int FurnitureSlot { get; set; } = 0;
}
