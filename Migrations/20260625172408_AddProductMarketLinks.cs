using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BahceFiyatTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMarketLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductMarketLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVarietyId = table.Column<int>(type: "int", nullable: false),
                    MarketId = table.Column<int>(type: "int", nullable: false),
                    DirectUrl = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMarketLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMarketLinks_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductMarketLinks_ProductVarieties_ProductVarietyId",
                        column: x => x.ProductVarietyId,
                        principalTable: "ProductVarieties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductMarketLinks_MarketId",
                table: "ProductMarketLinks",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMarketLinks_ProductVarietyId_MarketId_DirectUrl",
                table: "ProductMarketLinks",
                columns: new[] { "ProductVarietyId", "MarketId", "DirectUrl" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductMarketLinks");
        }
    }
}
