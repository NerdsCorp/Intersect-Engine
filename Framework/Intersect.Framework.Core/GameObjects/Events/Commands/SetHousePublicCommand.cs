namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class SetHousePublicCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.SetHousePublic;

    /// <summary>
    /// Whether to make the house public or private.
    /// </summary>
    public bool IsPublic { get; set; } = true;
}
