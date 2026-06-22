using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BahceFiyatTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddWebSearchMarket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Markets",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SearchUrlTemplate" },
                values: new object[] { 6, "https://search.brave.com", true, "Web Arama", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
