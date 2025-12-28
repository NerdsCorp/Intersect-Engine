using Intersect.Framework.Core.GameObjects.Maps;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public partial class PurchaseHouseCommand : EventCommand
{
    //For Json Deserialization
    public PurchaseHouseCommand()
    {
    }

    public PurchaseHouseCommand(Dictionary<Guid, List<EventCommand>> commandLists)
    {
        for (var i = 0; i < BranchIds.Length; i++)
        {
            BranchIds[i] = Guid.NewGuid();
            commandLists.Add(BranchIds[i], []);
        }
    }

    public override EventCommandType Type { get; } = EventCommandType.PurchaseHouse;

    /// <summary>
    /// The map ID to use as the house interior template.
    /// </summary>
    public Guid MapId { get; set; }

    /// <summary>
    /// The cost to purchase the house.
    /// </summary>
    public int Cost { get; set; }

    /// <summary>
    /// The currency/item ID required for purchase.
    /// </summary>
    public Guid CurrencyId { get; set; }

    //Branch[0] is the event commands to execute when house purchased successfully, Branch[1] is for when it fails.
    public Guid[] BranchIds { get; set; } = new Guid[2];

    public override string GetCopyData(
        Dictionary<Guid, List<EventCommand>> commandLists,
        Dictionary<Guid, List<EventCommand>> copyLists
    )
    {
        foreach (var branch in BranchIds)
        {
            if (branch != Guid.Empty && commandLists.ContainsKey(branch))
            {
                copyLists.Add(branch, commandLists[branch]);
                foreach (var cmd in commandLists[branch])
                {
                    cmd.GetCopyData(commandLists, copyLists);
                }
            }
        }

        return base.GetCopyData(commandLists, copyLists);
    }
}
