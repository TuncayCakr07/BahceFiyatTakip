using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BahceFiyatTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "IsActive", "Name", "Unit" },
                values: new object[,]
                {
                    { 14, "İşlenmiş", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Reçel & Marmelat", "Adet" },
                    { 15, "İşlenmiş", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Nar Ekşisi", "Şişe" },
                    { 16, "İşlenmiş", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Kurutulmuş Ürünler", "Gr" },
                    { 17, "İşlenmiş", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Meyve Suyu", "Şişe" }
                });

            migrationBuilder.InsertData(
                table: "ProductVarieties",
                columns: new[] { "Id", "HarvestPeriod", "IsActive", "Name", "Notes", "ProductId" },
                values: new object[,]
                {
                    { 52, null, true, "PORTAKAL REÇELİ", "Portakal kabuğu veya etinden yapılan reçel.", 14 },
                    { 53, null, true, "TURUNÇ REÇELİ", "Turunç kabuğu reçeli; acı aromalı.", 14 },
                    { 54, null, true, "BERGAMOT REÇELİ", "Bergamot kabuğu reçeli; yoğun aroma.", 14 },
                    { 55, null, true, "LİMON REÇELİ", "Limon kabuğu reçeli.", 14 },
                    { 56, null, true, "KUMKUAT REÇELİ", "Kumkuat reçeli; ekşi-tatlı.", 14 },
                    { 57, null, true, "NAR REÇELİ", "Nar reçeli veya nar tozu marmelatı.", 14 },
                    { 58, null, true, "MANDALİNA MARMELATI", "Mandalina marmelatı; tatlı-ekşi denge.", 14 },
                    { 59, null, true, "KARIŞIK MARMELAT", "Erik, armut, çarkıfelek vb. karışık marmelat.", 14 },
                    { 60, null, true, "NAR EKŞİSİ", "Sıkma nar ekşisi; doğal veya organik.", 15 },
                    { 61, null, true, "KURUTULMUŞ NARENÇİYE", "Portakal, limon, mandalina, greyfurt cipsi ve kurusu.", 16 },
                    { 62, null, true, "KAN PORTAKAL KURUSU", "Kurutulmuş kan portakalı dilimi.", 16 },
                    { 63, null, true, "NAR KURUSU", "Kurutulmuş nar tanesi.", 16 },
                    { 64, null, true, "KAFFİR LİME YAPRAĞI", "Kurutulmuş kaffir lime yaprağı; yemeklerde kullanılır.", 16 },
                    { 65, null, true, "LİME KURUSU", "Kurutulmuş lime dilimi veya freeze-dry.", 16 },
                    { 66, null, true, "NAR SUYU", "Taze sıkma veya şişelenmiş nar suyu.", 17 },
                    { 67, null, true, "LİMON SUYU", "Taze sıkma veya şişelenmiş limon suyu.", 17 }
                });

            migrationBuilder.InsertData(
                table: "ProductSearchAliases",
                columns: new[] { "Id", "Priority", "ProductVarietyId", "Query" },
                values: new object[,]
                {
                    { 205, 1, 52, "portakal receli" },
                    { 206, 2, 52, "portakal kabugu receli" },
                    { 209, 1, 53, "turunc receli" },
                    { 210, 2, 53, "turunc kabugu receli" },
                    { 213, 1, 54, "bergamot receli" },
                    { 214, 2, 54, "bergamot kabugu receli" },
                    { 217, 1, 55, "limon receli" },
                    { 218, 2, 55, "limon kabugu receli" },
                    { 221, 1, 56, "kumkuat receli" },
                    { 225, 1, 57, "nar receli" },
                    { 229, 1, 58, "mandalina marmelati" },
                    { 230, 2, 58, "mandalina receli" },
                    { 233, 1, 59, "erik marmelati" },
                    { 234, 2, 59, "carkifelek marmelati" },
                    { 235, 3, 59, "armut marmelati" },
                    { 237, 1, 60, "nar eksisi" },
                    { 238, 2, 60, "pomegranate molasses" },
                    { 239, 3, 60, "dogal nar eksisi" },
                    { 241, 1, 61, "portakal kurusu" },
                    { 242, 2, 61, "mandalina cipsi" },
                    { 243, 3, 61, "limon kurusu" },
                    { 245, 1, 62, "kan portakal kurusu" },
                    { 249, 1, 63, "nar kurusu" },
                    { 253, 1, 64, "kaffir lime yapragi" },
                    { 254, 2, 64, "kurutulmus lime yaprak" },
                    { 257, 1, 65, "lime kurusu" },
                    { 258, 2, 65, "freeze dry lime" },
                    { 261, 1, 66, "nar suyu" },
                    { 262, 2, 66, "pomegranate juice" },
                    { 265, 1, 67, "limon suyu" },
                    { 266, 2, 67, "lemon juice" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 205);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 209);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 210);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 213);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 214);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 217);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 218);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 221);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 225);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 229);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 230);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 233);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 234);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 235);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 237);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 238);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 239);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 241);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 242);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 243);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 245);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 249);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 253);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 254);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 257);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 258);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 261);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 262);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 265);

            migrationBuilder.DeleteData(
                table: "ProductSearchAliases",
                keyColumn: "Id",
                keyValue: 266);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17);
        }
    }
}
