using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BahceFiyatTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddNewProductsAndFixNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── MARKET GÜNCELLEMELERI ────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 1,
                column: "SearchUrlTemplate",
                value: "https://www.migros.com.tr/rest/products/search?q={0}&sayfa=0&sira=ONERILENLER&webSubdomain=www");

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsActive", "SearchUrlTemplate" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsActive",
                value: false);

            // ── ÜRÜN GRUBU ADI ───────────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Diğer Ürünler");

            // ── ÇEŞİT ADLARI / NOTLARI (Türkçe karakterler) ────────────────────
            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HarvestPeriod", "Name", "Notes" },
                values: new object[] { "Eylül sonu", "OKİTSU", "Çekirdeksiz; ilk dönem ekşimsi, sonra tatlılaşır; ilk hasatta yeşil." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Ekim sonu - Kasım", "Çekirdeksiz; sulu ve tatlı." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HarvestPeriod", "Name", "Notes" },
                values: new object[] { "Kasım sonu", "KLEMANTİN", "Çekirdekli; yoğun aromalı; kısa hasat dönemi." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "ORRİ", "Çok tatlı; sulu ve aromatik; az çekirdek görülebilir." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 5,
                column: "Notes",
                value: "Çekirdekli; yoğun tatlı, hafif ekşi; sert ve parlak kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Eylül başı", "Mandalina-limon aroması; az asitli, bol sulu." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "KIRMIZI LİMON", "Portakal ve limon karışımı aroma; çekirdekli; hafif ekşi." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Eylül sonu", "En ekşi limon; kalın kabuklu." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Aralık - Nisan", "Klasik limon; tam limon aroması; çay için ideal." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "HarvestPeriod", "Name", "Notes" },
                values: new object[] { "Mart başı", "KOKULU LİMON", "Limonata için ideal; çok sulu." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 11,
                column: "Notes",
                value: "İtalyan kökenli; kalın kabuklu; reçel ve içeceklerde kullanılır.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "TATLI LİMON", "Ekşilik yoktur; limonata için uygundur." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 13,
                column: "Notes",
                value: "İnce kabuklu; sulu; aroması yüksek.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 14,
                column: "Notes",
                value: "Çok yoğun aroma; az sulu; yemek ve soslarda kullanılır.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 15,
                column: "Notes",
                value: "Mandalinaya benzer; ekşi değil, tatlıdır.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 16,
                column: "Notes",
                value: "Kırmızı tonlu; nadir çeşit.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 17,
                column: "Notes",
                value: "Finger lime; keskin aroma; asidi yüksek; parlak yeşil taneler.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 19,
                column: "Notes",
                value: "Finger lime; sarı-yeşil; mat ve sert taneli.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Kasım başı", "Finike portakalı; çok tatlı; çekirdeksiz." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "HarvestPeriod", "Name", "Notes" },
                values: new object[] { "Nisan - Eylül", "VALENSİYA", "Çekirdekli; sıkmalık ve yemelik." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 23,
                column: "Notes",
                value: "Kırmızı etli; hafif ekşimsi; ince kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 24,
                column: "Notes",
                value: "Kan portakalı; pembe iç renk; tatlı; ince kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "ŞEKER PORTAKALI", "Çok tatlı; asidi düşük." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 26,
                column: "Notes",
                value: "Pütürlü avokado; en yüksek yağ oranı; güçlü aroma.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 27,
                column: "Notes",
                value: "Armut şekilli; parlak kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 28,
                column: "Notes",
                value: "İnce kabuklu; hafif lezzetli.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 29,
                column: "Notes",
                value: "Hafif pütürlü; ince kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 30,
                column: "Notes",
                value: "Yağ oranı düşük; sulu yapı.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 31,
                column: "Notes",
                value: "Meyvesi yenmez; kabuğu reçel ve çayda kullanılır.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 32,
                column: "Notes",
                value: "Kabuğuyla yenir; dışı tatlı, içi ekşi.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "ŞADOK", "Pomelo + greyfurt aroması; kalın kabuklu." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 34,
                column: "Notes",
                value: "Beyaz iç ve dış renk.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 35,
                column: "Notes",
                value: "Kırmızı iç renk; daha yumuşak tat.");

            // ── YENİ ÇEŞİTLER: LİMEKUAT & TURUNÇ ──────────────────────────────
            migrationBuilder.InsertData(
                table: "ProductVarieties",
                columns: new[] { "Id", "HarvestPeriod", "IsActive", "Name", "Notes", "ProductId" },
                values: new object[,]
                {
                    { 36, null, true, "LİMEKUAT", "Kumkuat-lime melezi; ekşi-tatlı denge.", 7 },
                    { 37, null, true, "TURUNÇ", "Acı portakal; reçel, likör ve parfümde kullanılır.", 7 }
                });

            // ── YENİ ÜRÜNLER ─────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "IsActive", "Name", "Unit" },
                values: new object[,]
                {
                    { 8,  "Meyve",    new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Nar",           "Kg"    },
                    { 9,  "Tropikal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Ejder Meyvesi", "Kg"    },
                    { 10, "Tropikal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Çarkıfelek",    "Kg"    },
                    { 11, "Tropikal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Mango",         "Kg"    },
                    { 12, "Sebze",    new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Domates",       "Kg"    },
                    { 13, "Diğer",    new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Limon Otu",     "Demet" }
                });

            // ── YENİ ÇEŞİTLER (nar, ejder, çarkıfelek, mango, domates, limon otu)
            migrationBuilder.InsertData(
                table: "ProductVarieties",
                columns: new[] { "Id", "HarvestPeriod", "IsActive", "Name", "Notes", "ProductId" },
                values: new object[,]
                {
                    { 38, "Eylül - Kasım", true, "HİCAZ",          "En yaygın Türk nar çeşidi; koyu kırmızı taneli; tatlı-ekşi.",    8  },
                    { 39, "Eylül",         true, "HICAZ 9 EYLÜL",  "Erkenci Hicaz; ince kabuklu.",                                   8  },
                    { 40, null,            true, "KIRMIZI EJDER",   "Kırmızı kabuklu; beyaz etli; hafif tatlı.",                      9  },
                    { 41, null,            true, "SARIMIN EJDER",   "Sarı kabuklu; beyaz etli; en tatlı çeşit.",                      9  },
                    { 42, null,            true, "MOR ÇARKIFELEK",  "Mor kabuklu; küçük; yoğun aroma.",                               10 },
                    { 43, null,            true, "SARI ÇARKIFELEK", "Sarı kabuklu; büyük; daha az yoğun.",                            10 },
                    { 44, "Temmuz - Eylül",true, "KENT",            "En yaygın Türkiye mangoso; tatlı ve lifli.",                     11 },
                    { 45, null,            true, "TOMMY ATKINS",    "Kırmızı-yeşil; lifli; uzun raf ömrü.",                           11 },
                    { 46, null,            true, "KEITT",           "Yeşil kabuk; sarı et; az lifli; tatlı.",                         11 },
                    { 47, null,            true, "SALKIMLI",        "Salkım domates; orta boy; dengeli tat.",                         12 },
                    { 48, null,            true, "SAN MARZANO",     "İtalyan çeşidi; salça ve soslar için ideal.",                    12 },
                    { 49, null,            true, "CHERRY DOMATES",  "Kiraz domates; tatlı ve sulu.",                                  12 },
                    { 50, null,            true, "TAZE LİMON OTU",  "Taze demet; yemek ve çay için.",                                 13 },
                    { 51, null,            true, "KURUTULMUŞ",      "Kurutulmuş limon otu; çay için.",                                13 }
                });

            // ── ARAMA ALIAS'LARI (yeni çeşitler için, varietyId*4-3 pattern) ───
            migrationBuilder.InsertData(
                table: "ProductSearchAliases",
                columns: new[] { "Id", "Priority", "ProductVarietyId", "Query" },
                values: new object[,]
                {
                    // v36 LİMEKUAT
                    { 141, 1, 36, "limekuat"              },
                    { 142, 2, 36, "limequat"              },
                    { 143, 3, 36, "limekuat kg"           },
                    // v37 TURUNÇ
                    { 145, 1, 37, "turunc"                },
                    { 146, 2, 37, "turunc portakal"       },
                    { 147, 3, 37, "aci portakal"          },
                    // v38 HİCAZ
                    { 149, 1, 38, "hicaz nar"             },
                    { 150, 2, 38, "hicaz nar kg"          },
                    { 151, 3, 38, "nar kg"                },
                    // v39 HICAZ 9 EYLÜL
                    { 153, 1, 39, "hicaz 9 eylul nar"    },
                    { 154, 2, 39, "erkenci nar"           },
                    // v40 KIRMIZI EJDER
                    { 157, 1, 40, "ejder meyvesi"         },
                    { 158, 2, 40, "kirmizi ejder meyvesi" },
                    { 159, 3, 40, "dragon fruit"          },
                    // v41 SARIMIN EJDER
                    { 161, 1, 41, "sari ejder meyvesi"    },
                    { 162, 2, 41, "yellow dragon fruit"   },
                    // v42 MOR ÇARKIFELEK
                    { 165, 1, 42, "carkifelek"            },
                    { 166, 2, 42, "passion fruit"         },
                    { 167, 3, 42, "carkifelek kg"         },
                    // v43 SARI ÇARKIFELEK
                    { 169, 1, 43, "sari carkifelek"       },
                    { 170, 2, 43, "yellow passion fruit"  },
                    // v44 KENT
                    { 173, 1, 44, "mango"                 },
                    { 174, 2, 44, "kent mango"            },
                    { 175, 3, 44, "mango kg"              },
                    // v45 TOMMY ATKINS
                    { 177, 1, 45, "tommy atkins mango"    },
                    { 178, 2, 45, "tommy mango kg"        },
                    // v46 KEITT
                    { 181, 1, 46, "keitt mango"           },
                    { 182, 2, 46, "yesil mango"           },
                    // v47 SALKIMLI
                    { 185, 1, 47, "salkimli domates"      },
                    { 186, 2, 47, "domates kg"            },
                    // v48 SAN MARZANO
                    { 189, 1, 48, "san marzano domates"   },
                    { 190, 2, 48, "san marzano kg"        },
                    // v49 CHERRY DOMATES
                    { 193, 1, 49, "cherry domates"        },
                    { 194, 2, 49, "kiraz domates"         },
                    // v50 TAZE LİMON OTU
                    { 197, 1, 50, "limon otu"             },
                    { 198, 2, 50, "lemon grass"           },
                    { 199, 3, 50, "limon otu demet"       },
                    // v51 KURUTULMUŞ
                    { 201, 1, 51, "kurutulmus limon otu"  },
                    { 202, 2, 51, "dried lemon grass"     }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── ALIAS'LARI SİL ──────────────────────────────────────────────────
            foreach (var id in new[] { 141, 142, 143, 145, 146, 147, 149, 150, 151,
                                       153, 154, 157, 158, 159, 161, 162, 165, 166, 167,
                                       169, 170, 173, 174, 175, 177, 178, 181, 182,
                                       185, 186, 189, 190, 193, 194, 197, 198, 199, 201, 202 })
            {
                migrationBuilder.DeleteData(table: "ProductSearchAliases", keyColumn: "Id", keyValue: id);
            }

            // ── YENİ ÇEŞİTLERİ SİL ─────────────────────────────────────────────
            foreach (var id in new[] { 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51 })
                migrationBuilder.DeleteData(table: "ProductVarieties", keyColumn: "Id", keyValue: id);

            // ── YENİ ÜRÜNLERİ SİL ──────────────────────────────────────────────
            foreach (var id in new[] { 8, 9, 10, 11, 12, 13 })
                migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: id);

            // ── LİMEKUAT & TURUNÇ'U SİL ─────────────────────────────────────────
            migrationBuilder.DeleteData(table: "ProductVarieties", keyColumn: "Id", keyValue: 36);
            migrationBuilder.DeleteData(table: "ProductVarieties", keyColumn: "Id", keyValue: 37);

            // ── ÜRÜN 7 ADINI GERİ AL ─────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Diger Urunler");

            // ── MARKET'LERI GERİ AL ─────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 1,
                column: "SearchUrlTemplate",
                value: "https://www.migros.com.tr/arama?q={0}");

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsActive", "SearchUrlTemplate" },
                values: new object[] { true, "https://www.bim.com.tr" });

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsActive",
                value: true);

            // ── ÇEŞİT ADLARI / NOTLARI (ASCII'ye geri al) ──────────────────────
            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HarvestPeriod", "Name", "Notes" },
                values: new object[] { "Eylul sonu", "OKITSU", "Cekirdeksiz; ilk donem eksimsi, sonra tatlilasir; ilk hasatta yesil." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Ekim sonu - Kasim", "Cekirdeksiz; sulu ve tatli." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HarvestPeriod", "Name", "Notes" },
                values: new object[] { "Kasim sonu", "KLEMANTIN", "Cekirdekli; yogun aromali; kisa hasat donemi." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "ORRI", "Cok tatli; sulu ve aromatik; az cekirdek gorulebilir." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 5,
                column: "Notes",
                value: "Cekirdekli; yogun tatli, hafif eksi; sert ve parlak kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Eylul basi", "Mandalina-limon aromasi; az asitli, bol sulu." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "KIRMIZI LIMON", "Portakal ve limon karisimi aroma; cekirdekli; hafif eksi." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Eylul sonu", "En eksi limon; kalin kabuklu." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Aralik - Nisan", "Klasik limon; tam limon aromasi; cay icin ideal." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "HarvestPeriod", "Name", "Notes" },
                values: new object[] { "Mart basi", "KOKULU LIMON", "Limonata icin ideal; cok sulu." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 11,
                column: "Notes",
                value: "Italyan kokenli; kalin kabuklu; recel ve iceceklerde kullanilir.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "TATLI LIMON", "Eksilik yoktur; limonata icin uygundur." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 13,
                column: "Notes",
                value: "Ince kabuklu; sulu; aromasi yuksek.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 14,
                column: "Notes",
                value: "Cok yogun aroma; az sulu; yemek ve soslarda kullanilir.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 15,
                column: "Notes",
                value: "Mandalinaya benzer; eksi degil, tatlidir.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 16,
                column: "Notes",
                value: "Kirmizi tonlu; nadir cesit.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 17,
                column: "Notes",
                value: "Finger lime; keskin aroma; asidi yuksek; parlak yesil taneler.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 19,
                column: "Notes",
                value: "Finger lime; sari-yesil; mat ve sert taneli.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "HarvestPeriod", "Notes" },
                values: new object[] { "Kasim basi", "Finike portakali; cok tatli; cekirdeksiz." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "HarvestPeriod", "Name", "Notes" },
                values: new object[] { "Nisan - Eylul", "VALENSIYA", "Cekirdekli; sikmalik ve yemelik." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 23,
                column: "Notes",
                value: "Kirmizi etli; hafif eksimsi; ince kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 24,
                column: "Notes",
                value: "Kan portakali; pembe ic renk; tatli; ince kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "SEKER PORTAKALI", "Cok tatli; asidi dusuk." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 26,
                column: "Notes",
                value: "Puturlu avokado; en yuksek yag orani; guclu aroma.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 27,
                column: "Notes",
                value: "Armut sekilli; parlak kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 28,
                column: "Notes",
                value: "Ince kabuklu; hafif lezzetli.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 29,
                column: "Notes",
                value: "Hafif puturlu; ince kabuklu.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 30,
                column: "Notes",
                value: "Yag orani dusuk; sulu yapi.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 31,
                column: "Notes",
                value: "Meyvesi yenmez; kabugu recel ve cayda kullanilir.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 32,
                column: "Notes",
                value: "Kabuguyla yenir; disi tatli, ici eksi.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Name", "Notes" },
                values: new object[] { "SADOK", "Pomelo + greyfurt aromasi; kalin kabuklu." });

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 34,
                column: "Notes",
                value: "Beyaz ic ve dis renk.");

            migrationBuilder.UpdateData(
                table: "ProductVarieties",
                keyColumn: "Id",
                keyValue: 35,
                column: "Notes",
                value: "Kirmizi ic renk; daha yumusak tat.");
        }
    }
}
