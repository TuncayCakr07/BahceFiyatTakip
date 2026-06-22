using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BahceFiyatTakip.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Markets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SearchUrlTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MarketId = table.Column<int>(type: "int", nullable: false),
                    PricePerKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsLive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceRecords_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceRecords_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Markets",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SearchUrlTemplate" },
                values: new object[,]
                {
                    { 1, "https://www.migros.com.tr", true, "Migros", "https://www.migros.com.tr/arama?q={0}" },
                    { 2, "https://www.a101.com.tr", true, "A101", "https://www.a101.com.tr/arama/?search_text={0}" },
                    { 3, "https://www.carrefoursa.com", true, "CarrefourSA", "https://www.carrefoursa.com/search/?text={0}" },
                    { 4, "https://www.sokmarket.com.tr", true, "Sok", "https://www.sokmarket.com.tr/arama?q={0}" },
                    { 5, "https://www.bim.com.tr", true, "BIM", "https://www.bim.com.tr" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Markets_Name",
                table: "Markets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceRecords_MarketId",
                table: "PriceRecords",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRecords_ProductId_MarketId_CheckedAt",
                table: "PriceRecords",
                columns: new[] { "ProductId", "MarketId", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceRecords");

            migrationBuilder.DropTable(
                name: "Markets");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
