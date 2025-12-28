using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Player
{
    /// <inheritdoc />
    public partial class AddPlayerHousingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add VisitingHouseId column to Players table
            migrationBuilder.AddColumn<Guid>(
                name: "VisitingHouseId",
                table: "Players",
                type: "TEXT",
                nullable: false,
                defaultValue: Guid.Empty);

            // Create PlayerHouses table
            migrationBuilder.CreateTable(
                name: "PlayerHouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MapId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    HouseName = table.Column<string>(type: "TEXT", nullable: true),
                    HouseDescription = table.Column<string>(type: "TEXT", nullable: true),
                    VisitCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    TotalRating = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    RatingCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerHouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerHouses_Players_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create House_Furniture table
            migrationBuilder.CreateTable(
                name: "House_Furniture",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    BagId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true),
                    X = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Y = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_House_Furniture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_House_Furniture_PlayerHouses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "PlayerHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create House_Visitors table
            migrationBuilder.CreateTable(
                name: "House_Visitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VisitorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Permission = table.Column<int>(type: "INTEGER", nullable: false),
                    InvitedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_House_Visitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_House_Visitors_PlayerHouses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "PlayerHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create Furniture_Storage table
            migrationBuilder.CreateTable(
                name: "Furniture_Storage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FurnitureSlotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SlotCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Furniture_Storage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Furniture_Storage_House_Furniture_FurnitureSlotId",
                        column: x => x.FurnitureSlotId,
                        principalTable: "House_Furniture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create Furniture_Storage_Slots table
            migrationBuilder.CreateTable(
                name: "Furniture_Storage_Slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StorageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    BagId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Furniture_Storage_Slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Furniture_Storage_Slots_Furniture_Storage_StorageId",
                        column: x => x.StorageId,
                        principalTable: "Furniture_Storage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_PlayerHouses_OwnerId",
                table: "PlayerHouses",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHouses_IsPublic",
                table: "PlayerHouses",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_House_Furniture_HouseId",
                table: "House_Furniture",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_House_Furniture_HouseId_Slot",
                table: "House_Furniture",
                columns: new[] { "HouseId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_House_Visitors_HouseId",
                table: "House_Visitors",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_House_Visitors_VisitorId",
                table: "House_Visitors",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_House_Visitors_HouseId_VisitorId",
                table: "House_Visitors",
                columns: new[] { "HouseId", "VisitorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Furniture_Storage_FurnitureSlotId",
                table: "Furniture_Storage",
                column: "FurnitureSlotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Furniture_Storage_Slots_StorageId",
                table: "Furniture_Storage_Slots",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_Furniture_Storage_Slots_StorageId_Slot",
                table: "Furniture_Storage_Slots",
                columns: new[] { "StorageId", "Slot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order
            migrationBuilder.DropTable(name: "Furniture_Storage_Slots");
            migrationBuilder.DropTable(name: "Furniture_Storage");
            migrationBuilder.DropTable(name: "House_Visitors");
            migrationBuilder.DropTable(name: "House_Furniture");
            migrationBuilder.DropTable(name: "PlayerHouses");

            // Drop column from Players table
            migrationBuilder.DropColumn(
                name: "VisitingHouseId",
                table: "Players");
        }
    }
}
