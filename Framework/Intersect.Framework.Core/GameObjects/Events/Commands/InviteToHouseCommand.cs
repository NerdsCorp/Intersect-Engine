using Intersect.Enums;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class InviteToHouseCommand : EventCommand
{
    public override EventCommandType Type { get; } = EventCommandType.InviteToHouse;

    /// <summary>
    /// Variable containing the name of the player to invite.
    /// </summary>
    public Guid PlayerVariableId { get; set; }

    /// <summary>
    /// The permission level to grant.
    /// </summary>
    public HousePermission Permission { get; set; } = HousePermission.View;
}
