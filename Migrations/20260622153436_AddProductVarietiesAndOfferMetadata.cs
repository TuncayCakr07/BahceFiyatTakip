using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BahceFiyatTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVarietiesAndOfferMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfidenceScore",
                table: "PriceRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "PriceRecords",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchedTitle",
                table: "PriceRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVarietyId",
                table: "PriceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductVarieties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HarvestPeriod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVarieties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVarieties_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSearchAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVarietyId = table.Column<int>(type: "int", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSearchAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSearchAliases_ProductVarieties_ProductVarietyId",
                        column: x => x.ProductVarietyId,
                        principalTable: "ProductVarieties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "IsActive", "Name", "Unit" },
                values: new object[,]
                {
                    { 1, "Narenciye", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Mandalina", "Kg" },
                    { 2, "Narenciye", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Limon", "Kg" },
                    { 3, "Narenciye", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Lime", "Kg" },
                    { 4, "Narenciye", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Finger Lime", "Kg" },
                    { 5, "Narenciye", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Portakal", "Kg" },
                    { 6, "Tropikal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Avokado", "Kg" },
                    { 7, "Narenciye", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Diger Urunler", "Kg" }
                });

            migrationBuilder.InsertData(
                table: "ProductVarieties",
                columns: new[] { "Id", "HarvestPeriod", "IsActive", "Name", "Notes", "ProductId" },
                values: new object[,]
                {
                    { 1, "Eylul sonu", true, "OKITSU", "Cekirdeksiz; ilk donem eksimsi, sonra tatlilasir; ilk hasatta yesil.", 1 },
                    { 2, "Ekim sonu - Kasim", true, "SATSUMA", "Cekirdeksiz; sulu ve tatli.", 1 },
                    { 3, "Kasim sonu", true, "KLEMANTIN", "Cekirdekli; yogun aromali; kisa hasat donemi.", 1 },
                    { 4, "Ocak sonu", true, "ORRI", "Cok tatli; sulu ve aromatik; az cekirdek gorulebilir.", 1 },
                    { 5, "Ocak sonu", true, "MURCOTT", "Cekirdekli; yogun tatli, hafif eksi; sert ve parlak kabuklu.", 1 },
                    { 6, "Eylul basi", true, "MAYER", "Mandalina-limon aromasi; az asitli, bol sulu.", 2 },
                    { 7, null, true, "KIRMIZI LIMON", "Portakal ve limon karisimi aroma; cekirdekli; hafif eksi.", 2 },
                    { 8, "Eylul sonu", true, "ENTERDONAT", "En eksi limon; kalin kabuklu.", 2 },
                    { 9, "Aralik - Nisan", true, "LAMAS", "Klasik limon; tam limon aromasi; cay icin ideal.", 2 },
                    { 10, "Mart basi", true, "KOKULU LIMON", "Limonata icin ideal; cok sulu.", 2 },
                    { 11, null, true, "GRANDE", "Italyan kokenli; kalin kabuklu; recel ve iceceklerde kullanilir.", 2 },
                    { 12, null, true, "TATLI LIMON", "Eksilik yoktur; limonata icin uygundur.", 2 },
                    { 13, null, true, "LIME", "Ince kabuklu; sulu; aromasi yuksek.", 3 },
                    { 14, null, true, "KAFFIR LIME", "Cok yogun aroma; az sulu; yemek ve soslarda kullanilir.", 3 },
                    { 15, null, true, "TATLI LIME", "Mandalinaya benzer; eksi degil, tatlidir.", 3 },
                    { 16, null, true, "KAN LIME", "Kirmizi tonlu; nadir cesit.", 3 },
                    { 17, null, true, "VERDE", "Finger lime; keskin aroma; asidi yuksek; parlak yesil taneler.", 4 },
                    { 18, null, true, "ROSE", "Finger lime; pembe taneli; asidi dengeli; daha sulu.", 4 },
                    { 19, null, true, "SUNPEARL", "Finger lime; sari-yesil; mat ve sert taneli.", 4 },
                    { 20, null, true, "BLOSSOM", "Finger lime; uzun ve iri taneli.", 4 },
                    { 21, "Kasim basi", true, "WASHINGTON", "Finike portakali; cok tatli; cekirdeksiz.", 5 },
                    { 22, "Nisan - Eylul", true, "VALENSIYA", "Cekirdekli; sikmalik ve yemelik.", 5 },
                    { 23, null, true, "MYSTICRIMSON", "Kirmizi etli; hafif eksimsi; ince kabuklu.", 5 },
                    { 24, null, true, "BLUSHSWEET", "Kan portakali; pembe ic renk; tatli; ince kabuklu.", 5 },
                    { 25, null, true, "SEKER PORTAKALI", "Cok tatli; asidi dusuk.", 5 },
                    { 26, null, true, "HASS", "Puturlu avokado; en yuksek yag orani; guclu aroma.", 6 },
                    { 27, null, true, "ETTINGER", "Armut sekilli; parlak kabuklu.", 6 },
                    { 28, null, true, "BACON", "Ince kabuklu; hafif lezzetli.", 6 },
                    { 29, null, true, "FUERTE", "Hafif puturlu; ince kabuklu.", 6 },
                    { 30, null, true, "CLIFFTON", "Yag orani dusuk; sulu yapi.", 6 },
                    { 31, null, true, "BERGAMOT", "Meyvesi yenmez; kabugu recel ve cayda kullanilir.", 7 },
                    { 32, null, true, "KUMKUAT", "Kabuguyla yenir; disi tatli, ici eksi.", 7 },
                    { 33, null, true, "SADOK", "Pomelo + greyfurt aromasi; kalin kabuklu.", 7 },
                    { 34, null, true, "BEYAZ GREYFURT", "Beyaz ic ve dis renk.", 7 },
                    { 35, null, true, "KIRMIZI GREYFURT", "Kirmizi ic renk; daha yumusak tat.", 7 }
                });

            migrationBuilder.InsertData(
                table: "ProductSearchAliases",
                columns: new[] { "Id", "Priority", "ProductVarietyId", "Query" },
                values: new object[,]
                {
                    { 1, 1, 1, "okitsu mandalina" },
                    { 2, 2, 1, "okitsu mandalina kg" },
                    { 3, 3, 1, "okitsu fiyat" },
                    { 4, 4, 1, "okitsu fidan meyve" },
                    { 5, 1, 2, "satsuma mandalina" },
                    { 6, 2, 2, "satsuma mandalina kg" },
                    { 7, 3, 2, "satsuma fiyat" },
                    { 9, 1, 3, "klemantin mandalina" },
                    { 10, 2, 3, "clementine mandalina" },
                    { 11, 3, 3, "klemantin fiyat" },
                    { 13, 1, 4, "orri mandalina" },
                    { 14, 2, 4, "orri mandarin" },
                    { 15, 3, 4, "orri fiyat" },
                    { 17, 1, 5, "murcott mandalina" },
                    { 18, 2, 5, "murcott mandarin" },
                    { 19, 3, 5, "murcott fiyat" },
                    { 21, 1, 6, "mayer limon" },
                    { 22, 2, 6, "meyer limon" },
                    { 23, 3, 6, "mayer limon kg" },
                    { 25, 1, 7, "kirmizi limon" },
                    { 26, 2, 7, "red lemon" },
                    { 27, 3, 7, "kirmizi limon fiyat" },
                    { 29, 1, 8, "enterdonat limon" },
                    { 30, 2, 8, "enterdonat limon kg" },
                    { 31, 3, 8, "enternonat limon" },
                    { 33, 1, 9, "lamas limon" },
                    { 34, 2, 9, "klasik limon" },
                    { 35, 3, 9, "lamas limon kg" },
                    { 37, 1, 10, "kokulu limon" },
                    { 38, 2, 10, "kokulu limon kg" },
                    { 39, 3, 10, "limonata limonu" },
                    { 41, 1, 11, "grande limon" },
                    { 42, 2, 11, "italyan limon" },
                    { 43, 3, 11, "grande limon fiyat" },
                    { 45, 1, 12, "tatli limon" },
                    { 46, 2, 12, "sweet lemon" },
                    { 47, 3, 12, "tatli limon fiyat" },
                    { 49, 1, 13, "lime" },
                    { 50, 2, 13, "lime kg" },
                    { 51, 3, 13, "yesil limon" },
                    { 53, 1, 14, "kaffir lime" },
                    { 54, 2, 14, "kaffir lime fiyat" },
                    { 55, 3, 14, "kaffir limon" },
                    { 57, 1, 15, "tatli lime" },
                    { 58, 2, 15, "sweet lime" },
                    { 59, 3, 15, "tatli lime fiyat" },
                    { 61, 1, 16, "kan lime" },
                    { 62, 2, 16, "blood lime" },
                    { 63, 3, 16, "kan lime fiyat" },
                    { 65, 1, 17, "verde finger lime" },
                    { 66, 2, 17, "finger lime verde" },
                    { 67, 3, 17, "havyar limon verde" },
                    { 69, 1, 18, "rose finger lime" },
                    { 70, 2, 18, "finger lime rose" },
                    { 71, 3, 18, "pembe havyar limon" },
                    { 73, 1, 19, "sunpearl finger lime" },
                    { 74, 2, 19, "finger lime sunpearl" },
                    { 75, 3, 19, "sunpearl havyar limon" },
                    { 77, 1, 20, "blossom finger lime" },
                    { 78, 2, 20, "finger lime blossom" },
                    { 79, 3, 20, "blossom havyar limon" },
                    { 81, 1, 21, "washington portakal" },
                    { 82, 2, 21, "finike portakal" },
                    { 83, 3, 21, "washington portakal kg" },
                    { 85, 1, 22, "valensiya portakal" },
                    { 86, 2, 22, "valencia portakal" },
                    { 87, 3, 22, "valensiya portakal kg" },
                    { 89, 1, 23, "mysticrimson portakal" },
                    { 90, 2, 23, "mystic crimson orange" },
                    { 91, 3, 23, "kirmizi etli portakal" },
                    { 93, 1, 24, "blushsweet portakal" },
                    { 94, 2, 24, "kan portakali blushsweet" },
                    { 95, 3, 24, "pembe portakal" },
                    { 97, 1, 25, "seker portakali" },
                    { 98, 2, 25, "tatli portakal" },
                    { 99, 3, 25, "seker portakal kg" },
                    { 101, 1, 26, "hass avokado" },
                    { 102, 2, 26, "puturlu avokado" },
                    { 103, 3, 26, "hass avokado kg" },
                    { 105, 1, 27, "ettinger avokado" },
                    { 106, 2, 27, "ettinger avocado" },
                    { 107, 3, 27, "ettinger avokado kg" },
                    { 109, 1, 28, "bacon avokado" },
                    { 110, 2, 28, "bacon avocado" },
                    { 111, 3, 28, "bacon avokado kg" },
                    { 113, 1, 29, "fuerte avokado" },
                    { 114, 2, 29, "fuerte avocado" },
                    { 115, 3, 29, "fuerte avokado kg" },
                    { 117, 1, 30, "cliffton avokado" },
                    { 118, 2, 30, "clifton avokado" },
                    { 119, 3, 30, "cliffton avocado" },
                    { 121, 1, 31, "bergamot" },
                    { 122, 2, 31, "bergamot kg" },
                    { 123, 3, 31, "bergamot fiyat" },
                    { 125, 1, 32, "kumkuat" },
                    { 126, 2, 32, "kamkat" },
                    { 127, 3, 32, "kumkuat kg" },
                    { 129, 1, 33, "sadok" },
                    { 130, 2, 33, "pomelo greyfurt" },
                    { 131, 3, 33, "sadok fiyat" },
                    { 133, 1, 34, "beyaz greyfurt" },
                    { 134, 2, 34, "white grapefruit" },
                    { 135, 3, 34, "beyaz greyfurt kg" },
                    { 137, 1, 35, "kirmizi greyfurt" },
                    { 138, 2, 35, "red grapefruit" },
                    { 139, 3, 35, "kirmizi greyfurt kg" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceRecords_ProductVarietyId",
                table: "PriceRecords",
                column: "ProductVarietyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSearchAliases_ProductVarietyId_Query",
                table: "ProductSearchAliases",
                columns: new[] { "ProductVarietyId", "Query" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVarieties_ProductId_Name",
                table: "ProductVarieties",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceRecords_ProductVarieties_ProductVarietyId",
                table: "PriceRecords",
                column: "ProductVarietyId",
                principalTable: "ProductVarieties",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceRecords_ProductVarieties_ProductVarietyId",
                table: "PriceRecords");

            migrationBuilder.DropTable(
                name: "ProductSearchAliases");

            migrationBuilder.DropTable(
                name: "ProductVarieties");

            migrationBuilder.DropIndex(
                name: "IX_PriceRecords_ProductVarietyId",
                table: "PriceRecords");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "PriceRecords");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "PriceRecords");

            migrationBuilder.DropColumn(
                name: "MatchedTitle",
                table: "PriceRecords");

            migrationBuilder.DropColumn(
                name: "ProductVarietyId",
                table: "PriceRecords");
        }
    }
}
