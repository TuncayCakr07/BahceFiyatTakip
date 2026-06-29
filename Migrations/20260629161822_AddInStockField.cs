using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BahceFiyatTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddInStockField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InStock",
                table: "PriceRecords",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InStock",
                table: "PriceRecords");
        }
    }
}
