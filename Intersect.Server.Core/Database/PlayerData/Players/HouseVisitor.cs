using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Intersect.Server.Database.PlayerData.Players;

/// <summary>
/// Represents a visitor who has been granted access to a player's house.
/// </summary>
public partial class HouseVisitor
{
    public HouseVisitor()
    {
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore]
    public Guid Id { get; private set; }

    /// <summary>
    /// The house this visitor belongs to.
    /// </summary>
    [JsonIgnore]
    public Guid HouseId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(HouseId))]
    public virtual PlayerHouse House { get; set; }

    /// <summary>
    /// The player ID who has visitor access.
    /// </summary>
    public Guid VisitorId { get; set; }

    /// <summary>
    /// The permission level for this visitor.
    /// </summary>
    public HousePermission Permission { get; set; }

    /// <summary>
    /// The date this visitor was invited.
    /// </summary>
    public DateTime InvitedDate { get; set; }
}
