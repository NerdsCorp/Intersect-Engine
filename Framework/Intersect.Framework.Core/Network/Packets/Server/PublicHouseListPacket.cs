using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class PublicHouseListPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public PublicHouseListPacket()
    {
    }

    public PublicHouseListPacket(PublicHouseInfo[] houses, int totalCount)
    {
        Houses = houses;
        TotalCount = totalCount;
    }

    [Key(0)]
    public PublicHouseInfo[] Houses { get; set; }

    [Key(1)]
    public int TotalCount { get; set; }
}

[MessagePackObject]
public partial class PublicHouseInfo
{
    [Key(0)]
    public Guid HouseId { get; set; }

    [Key(1)]
    public Guid OwnerId { get; set; }

    [Key(2)]
    public string OwnerName { get; set; }

    [Key(3)]
    public string HouseName { get; set; }

    [Key(4)]
    public string HouseDescription { get; set; }

    [Key(5)]
    public int VisitCount { get; set; }

    [Key(6)]
    public double AverageRating { get; set; }

    [Key(7)]
    public int RatingCount { get; set; }
}
