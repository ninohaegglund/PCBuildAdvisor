using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCBuildAdvisor.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    PerformanceTier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Socket = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemoryType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormFactor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportedSockets = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportedMemoryTypes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportedFormFactors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PowerConsumptionWatts = table.Column<int>(type: "int", nullable: true),
                    Wattage = table.Column<int>(type: "int", nullable: true),
                    LengthMm = table.Column<int>(type: "int", nullable: true),
                    HeightMm = table.Column<int>(type: "int", nullable: true),
                    CapacityGb = table.Column<int>(type: "int", nullable: true),
                    MemoryCapacityGb = table.Column<int>(type: "int", nullable: true),
                    MemorySlots = table.Column<int>(type: "int", nullable: true),
                    MaxMemoryCapacityGb = table.Column<int>(type: "int", nullable: true),
                    StorageInterface = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Genre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Demand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_ModelNumber",
                table: "Components",
                column: "ModelNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Components_Type",
                table: "Components",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
