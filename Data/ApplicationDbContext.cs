using BahceFiyatTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<PriceRecord> PriceRecords => Set<PriceRecord>();
    public DbSet<ProductVariety> ProductVarieties => Set<ProductVariety>();
    public DbSet<ProductSearchAlias> ProductSearchAliases => Set<ProductSearchAlias>();
    public DbSet<ProductMarketLink> ProductMarketLinks => Set<ProductMarketLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().HasIndex(p => p.Name).IsUnique();
        modelBuilder.Entity<Market>().HasIndex(m => m.Name).IsUnique();
        modelBuilder.Entity<PriceRecord>().HasIndex(r => new { r.ProductId, r.MarketId, r.CheckedAt });
        modelBuilder.Entity<ProductVariety>().HasIndex(v => new { v.ProductId, v.Name }).IsUnique();
        modelBuilder.Entity<ProductSearchAlias>().HasIndex(a => new { a.ProductVarietyId, a.Query }).IsUnique();
        modelBuilder.Entity<ProductMarketLink>().HasIndex(l => new { l.ProductVarietyId, l.MarketId, l.DirectUrl }).IsUnique();

        modelBuilder.Entity<Market>().HasData(
            new Market { Id = 1, Name = "Migros",     BaseUrl = "https://www.migros.com.tr",      IsActive = true,  SearchUrlTemplate = "https://www.migros.com.tr/rest/products/search?q={0}&sayfa=0&sira=ONERILENLER&webSubdomain=www" },
            new Market { Id = 2, Name = "A101",       BaseUrl = "https://www.a101.com.tr",        IsActive = true,  SearchUrlTemplate = "https://www.a101.com.tr/arama/?search_text={0}" },
            new Market { Id = 3, Name = "CarrefourSA",BaseUrl = "https://www.carrefoursa.com",   IsActive = true,  SearchUrlTemplate = "https://www.carrefoursa.com/search/?text={0}" },
            new Market { Id = 4, Name = "Sok",        BaseUrl = "https://www.sokmarket.com.tr",  IsActive = true,  SearchUrlTemplate = "https://www.sokmarket.com.tr/arama?q={0}" },
            new Market { Id = 5, Name = "BIM",        BaseUrl = "https://www.bim.com.tr",        IsActive = false, SearchUrlTemplate = null },
            new Market { Id = 6, Name = "Web Arama",  BaseUrl = "https://search.brave.com",      IsActive = false, SearchUrlTemplate = null });

        // ── ÜRÜNLER ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1,  Name = "Mandalina",      Category = "Narenciye", Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 2,  Name = "Limon",          Category = "Narenciye", Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 3,  Name = "Lime",           Category = "Narenciye", Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 4,  Name = "Finger Lime",    Category = "Narenciye", Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 5,  Name = "Portakal",       Category = "Narenciye", Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 6,  Name = "Avokado",        Category = "Tropikal",  Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 7,  Name = "Diğer Ürünler",  Category = "Narenciye", Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 8,  Name = "Nar",            Category = "Meyve",     Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 9,  Name = "Ejder Meyvesi",  Category = "Tropikal",  Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 10, Name = "Çarkıfelek",     Category = "Tropikal",  Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 11, Name = "Mango",          Category = "Tropikal",  Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 12, Name = "Domates",        Category = "Sebze",     Unit = "Kg",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 13, Name = "Limon Otu",      Category = "Diğer",     Unit = "Demet", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 14, Name = "Reçel & Marmelat",   Category = "İşlenmiş",  Unit = "Adet",  IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 15, Name = "Nar Ekşisi",          Category = "İşlenmiş",  Unit = "Şişe",  IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 16, Name = "Kurutulmuş Ürünler",  Category = "İşlenmiş",  Unit = "Gr",    IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 17, Name = "Meyve Suyu",          Category = "İşlenmiş",  Unit = "Şişe",  IsActive = true, CreatedAt = new DateTime(2026, 1, 1) });

        // ── ÇEŞİTLER ─────────────────────────────────────────────────────────
        modelBuilder.Entity<ProductVariety>().HasData(
            // Mandalina
            new ProductVariety { Id = 1,  ProductId = 1, Name = "OKİTSU",           HarvestPeriod = "Eylül sonu",         Notes = "Çekirdeksiz; ilk dönem ekşimsi, sonra tatlılaşır; ilk hasatta yeşil." },
            new ProductVariety { Id = 2,  ProductId = 1, Name = "SATSUMA",           HarvestPeriod = "Ekim sonu - Kasım",  Notes = "Çekirdeksiz; sulu ve tatlı." },
            new ProductVariety { Id = 3,  ProductId = 1, Name = "KLEMANTİN",         HarvestPeriod = "Kasım sonu",         Notes = "Çekirdekli; yoğun aromalı; kısa hasat dönemi." },
            new ProductVariety { Id = 4,  ProductId = 1, Name = "ORRİ",              HarvestPeriod = "Ocak sonu",          Notes = "Çok tatlı; sulu ve aromatik; az çekirdek görülebilir." },
            new ProductVariety { Id = 5,  ProductId = 1, Name = "MURCOTT",           HarvestPeriod = "Ocak sonu",          Notes = "Çekirdekli; yoğun tatlı, hafif ekşi; sert ve parlak kabuklu." },
            // Limon
            new ProductVariety { Id = 6,  ProductId = 2, Name = "MAYER",             HarvestPeriod = "Eylül başı",         Notes = "Mandalina-limon aroması; az asitli, bol sulu." },
            new ProductVariety { Id = 7,  ProductId = 2, Name = "KIRMIZI LİMON",     Notes = "Portakal ve limon karışımı aroma; çekirdekli; hafif ekşi." },
            new ProductVariety { Id = 8,  ProductId = 2, Name = "ENTERDONAT",        HarvestPeriod = "Eylül sonu",         Notes = "En ekşi limon; kalın kabuklu." },
            new ProductVariety { Id = 9,  ProductId = 2, Name = "LAMAS",             HarvestPeriod = "Aralık - Nisan",     Notes = "Klasik limon; tam limon aroması; çay için ideal." },
            new ProductVariety { Id = 10, ProductId = 2, Name = "KOKULU LİMON",      HarvestPeriod = "Mart başı",          Notes = "Limonata için ideal; çok sulu." },
            new ProductVariety { Id = 11, ProductId = 2, Name = "GRANDE",            Notes = "İtalyan kökenli; kalın kabuklu; reçel ve içeceklerde kullanılır." },
            new ProductVariety { Id = 12, ProductId = 2, Name = "TATLI LİMON",       Notes = "Ekşilik yoktur; limonata için uygundur." },
            // Lime
            new ProductVariety { Id = 13, ProductId = 3, Name = "LIME",              Notes = "İnce kabuklu; sulu; aroması yüksek." },
            new ProductVariety { Id = 14, ProductId = 3, Name = "KAFFIR LIME",       Notes = "Çok yoğun aroma; az sulu; yemek ve soslarda kullanılır." },
            new ProductVariety { Id = 15, ProductId = 3, Name = "TATLI LIME",        Notes = "Mandalinaya benzer; ekşi değil, tatlıdır." },
            new ProductVariety { Id = 16, ProductId = 3, Name = "KAN LIME",          Notes = "Kırmızı tonlu; nadir çeşit." },
            // Finger Lime
            new ProductVariety { Id = 17, ProductId = 4, Name = "VERDE",             Notes = "Finger lime; keskin aroma; asidi yüksek; parlak yeşil taneler." },
            new ProductVariety { Id = 18, ProductId = 4, Name = "ROSE",              Notes = "Finger lime; pembe taneli; asidi dengeli; daha sulu." },
            new ProductVariety { Id = 19, ProductId = 4, Name = "SUNPEARL",          Notes = "Finger lime; sarı-yeşil; mat ve sert taneli." },
            new ProductVariety { Id = 20, ProductId = 4, Name = "BLOSSOM",           Notes = "Finger lime; uzun ve iri taneli." },
            // Portakal
            new ProductVariety { Id = 21, ProductId = 5, Name = "WASHINGTON",        HarvestPeriod = "Kasım başı",         Notes = "Finike portakalı; çok tatlı; çekirdeksiz." },
            new ProductVariety { Id = 22, ProductId = 5, Name = "VALENSİYA",         HarvestPeriod = "Nisan - Eylül",      Notes = "Çekirdekli; sıkmalık ve yemelik." },
            new ProductVariety { Id = 23, ProductId = 5, Name = "MYSTICRIMSON",      Notes = "Kırmızı etli; hafif ekşimsi; ince kabuklu." },
            new ProductVariety { Id = 24, ProductId = 5, Name = "BLUSHSWEET",        Notes = "Kan portakalı; pembe iç renk; tatlı; ince kabuklu." },
            new ProductVariety { Id = 25, ProductId = 5, Name = "ŞEKER PORTAKALI",   Notes = "Çok tatlı; asidi düşük." },
            // Avokado
            new ProductVariety { Id = 26, ProductId = 6, Name = "HASS",              Notes = "Pütürlü avokado; en yüksek yağ oranı; güçlü aroma." },
            new ProductVariety { Id = 27, ProductId = 6, Name = "ETTINGER",          Notes = "Armut şekilli; parlak kabuklu." },
            new ProductVariety { Id = 28, ProductId = 6, Name = "BACON",             Notes = "İnce kabuklu; hafif lezzetli." },
            new ProductVariety { Id = 29, ProductId = 6, Name = "FUERTE",            Notes = "Hafif pütürlü; ince kabuklu." },
            new ProductVariety { Id = 30, ProductId = 6, Name = "CLIFFTON",          Notes = "Yağ oranı düşük; sulu yapı." },
            // Diğer Narenciye
            new ProductVariety { Id = 31, ProductId = 7, Name = "BERGAMOT",          Notes = "Meyvesi yenmez; kabuğu reçel ve çayda kullanılır." },
            new ProductVariety { Id = 32, ProductId = 7, Name = "KUMKUAT",           Notes = "Kabuğuyla yenir; dışı tatlı, içi ekşi." },
            new ProductVariety { Id = 33, ProductId = 7, Name = "ŞADOK",             Notes = "Pomelo + greyfurt aroması; kalın kabuklu." },
            new ProductVariety { Id = 34, ProductId = 7, Name = "BEYAZ GREYFURT",    Notes = "Beyaz iç ve dış renk." },
            new ProductVariety { Id = 35, ProductId = 7, Name = "KIRMIZI GREYFURT",  Notes = "Kırmızı iç renk; daha yumuşak tat." },
            new ProductVariety { Id = 36, ProductId = 7, Name = "LİMEKUAT",          Notes = "Kumkuat-lime melezi; ekşi-tatlı denge." },
            new ProductVariety { Id = 37, ProductId = 7, Name = "TURUNÇ",            Notes = "Acı portakal; reçel, likör ve parfümde kullanılır." },
            // Nar
            new ProductVariety { Id = 38, ProductId = 8, Name = "HİCAZ",             HarvestPeriod = "Eylül - Kasım",      Notes = "En yaygın Türk nar çeşidi; koyu kırmızı taneli; tatlı-ekşi." },
            new ProductVariety { Id = 39, ProductId = 8, Name = "HICAZ 9 EYLÜL",    HarvestPeriod = "Eylül",              Notes = "Erkenci Hicaz; ince kabuklu." },
            // Ejder Meyvesi
            new ProductVariety { Id = 40, ProductId = 9, Name = "KIRMIZI EJDER",     Notes = "Kırmızı kabuklu; beyaz etli; hafif tatlı." },
            new ProductVariety { Id = 41, ProductId = 9, Name = "SARIMIN EJDER",     Notes = "Sarı kabuklu; beyaz etli; en tatlı çeşit." },
            // Çarkıfelek
            new ProductVariety { Id = 42, ProductId = 10, Name = "MOR ÇARKIFELEK",   Notes = "Mor kabuklu; küçük; yoğun aroma." },
            new ProductVariety { Id = 43, ProductId = 10, Name = "SARI ÇARKIFELEK",  Notes = "Sarı kabuklu; büyük; daha az yoğun." },
            // Mango
            new ProductVariety { Id = 44, ProductId = 11, Name = "KENT",             HarvestPeriod = "Temmuz - Eylül",     Notes = "En yaygın Türkiye mangoso; tatlı ve lifli." },
            new ProductVariety { Id = 45, ProductId = 11, Name = "TOMMY ATKINS",     Notes = "Kırmızı-yeşil; lifli; uzun raf ömrü." },
            new ProductVariety { Id = 46, ProductId = 11, Name = "KEITT",            Notes = "Yeşil kabuk; sarı et; az lifli; tatlı." },
            // Domates
            new ProductVariety { Id = 47, ProductId = 12, Name = "SALKIMLI",         Notes = "Salkım domates; orta boy; dengeli tat." },
            new ProductVariety { Id = 48, ProductId = 12, Name = "SAN MARZANO",      Notes = "İtalyan çeşidi; salça ve soslar için ideal." },
            new ProductVariety { Id = 49, ProductId = 12, Name = "CHERRY DOMATES",   Notes = "Kiraz domates; tatlı ve sulu." },
            // Limon Otu
            new ProductVariety { Id = 50, ProductId = 13, Name = "TAZE LİMON OTU",  Notes = "Taze demet; yemek ve çay için." },
            new ProductVariety { Id = 51, ProductId = 13, Name = "KURUTULMUŞ",       Notes = "Kurutulmuş limon otu; çay için." },
            // Reçel & Marmelat
            new ProductVariety { Id = 52, ProductId = 14, Name = "PORTAKAL REÇELİ",     Notes = "Portakal kabuğu veya etinden yapılan reçel." },
            new ProductVariety { Id = 53, ProductId = 14, Name = "TURUNÇ REÇELİ",        Notes = "Turunç kabuğu reçeli; acı aromalı." },
            new ProductVariety { Id = 54, ProductId = 14, Name = "BERGAMOT REÇELİ",      Notes = "Bergamot kabuğu reçeli; yoğun aroma." },
            new ProductVariety { Id = 55, ProductId = 14, Name = "LİMON REÇELİ",         Notes = "Limon kabuğu reçeli." },
            new ProductVariety { Id = 56, ProductId = 14, Name = "KUMKUAT REÇELİ",       Notes = "Kumkuat reçeli; ekşi-tatlı." },
            new ProductVariety { Id = 57, ProductId = 14, Name = "NAR REÇELİ",           Notes = "Nar reçeli veya nar tozu marmelatı." },
            new ProductVariety { Id = 58, ProductId = 14, Name = "MANDALİNA MARMELATI",  Notes = "Mandalina marmelatı; tatlı-ekşi denge." },
            new ProductVariety { Id = 59, ProductId = 14, Name = "KARIŞIK MARMELAT",     Notes = "Erik, armut, çarkıfelek vb. karışık marmelat." },
            // Nar Ekşisi
            new ProductVariety { Id = 60, ProductId = 15, Name = "NAR EKŞİSİ",           Notes = "Sıkma nar ekşisi; doğal veya organik." },
            // Kurutulmuş Ürünler
            new ProductVariety { Id = 61, ProductId = 16, Name = "KURUTULMUŞ NARENÇİYE", Notes = "Portakal, limon, mandalina, greyfurt cipsi ve kurusu." },
            new ProductVariety { Id = 62, ProductId = 16, Name = "KAN PORTAKAL KURUSU",  Notes = "Kurutulmuş kan portakalı dilimi." },
            new ProductVariety { Id = 63, ProductId = 16, Name = "NAR KURUSU",            Notes = "Kurutulmuş nar tanesi." },
            new ProductVariety { Id = 64, ProductId = 16, Name = "KAFFİR LİME YAPRAĞI",  Notes = "Kurutulmuş kaffir lime yaprağı; yemeklerde kullanılır." },
            new ProductVariety { Id = 65, ProductId = 16, Name = "LİME KURUSU",           Notes = "Kurutulmuş lime dilimi veya freeze-dry." },
            // Meyve Suyu
            new ProductVariety { Id = 66, ProductId = 17, Name = "NAR SUYU",              Notes = "Taze sıkma veya şişelenmiş nar suyu." },
            new ProductVariety { Id = 67, ProductId = 17, Name = "LİMON SUYU",            Notes = "Taze sıkma veya şişelenmiş limon suyu." });

        modelBuilder.Entity<ProductSearchAlias>().HasData(BuildAllAliases());
    }

    private static ProductSearchAlias[] BuildAllAliases()
    {
        // ID pattern: varietyId * 4 - 3 = slot 1 (matches original migration)
        return
        [
            // Mandalina (IDs 1-20, 4 slots each)
            new ProductSearchAlias { Id = 1,  ProductVarietyId = 1,  Query = "okitsu mandalina",          Priority = 1 },
            new ProductSearchAlias { Id = 2,  ProductVarietyId = 1,  Query = "okitsu mandalina kg",       Priority = 2 },
            new ProductSearchAlias { Id = 3,  ProductVarietyId = 1,  Query = "okitsu fiyat",              Priority = 3 },
            new ProductSearchAlias { Id = 4,  ProductVarietyId = 1,  Query = "okitsu fidan meyve",        Priority = 4 },
            new ProductSearchAlias { Id = 5,  ProductVarietyId = 2,  Query = "satsuma mandalina",         Priority = 1 },
            new ProductSearchAlias { Id = 6,  ProductVarietyId = 2,  Query = "satsuma mandalina kg",      Priority = 2 },
            new ProductSearchAlias { Id = 7,  ProductVarietyId = 2,  Query = "satsuma fiyat",             Priority = 3 },
            new ProductSearchAlias { Id = 9,  ProductVarietyId = 3,  Query = "klemantin mandalina",       Priority = 1 },
            new ProductSearchAlias { Id = 10, ProductVarietyId = 3,  Query = "clementine mandalina",      Priority = 2 },
            new ProductSearchAlias { Id = 11, ProductVarietyId = 3,  Query = "klemantin fiyat",           Priority = 3 },
            new ProductSearchAlias { Id = 13, ProductVarietyId = 4,  Query = "orri mandalina",            Priority = 1 },
            new ProductSearchAlias { Id = 14, ProductVarietyId = 4,  Query = "orri mandarin",             Priority = 2 },
            new ProductSearchAlias { Id = 15, ProductVarietyId = 4,  Query = "orri fiyat",                Priority = 3 },
            new ProductSearchAlias { Id = 17, ProductVarietyId = 5,  Query = "murcott mandalina",         Priority = 1 },
            new ProductSearchAlias { Id = 18, ProductVarietyId = 5,  Query = "murcott mandarin",          Priority = 2 },
            new ProductSearchAlias { Id = 19, ProductVarietyId = 5,  Query = "murcott fiyat",             Priority = 3 },
            // Limon (IDs 21-48)
            new ProductSearchAlias { Id = 21, ProductVarietyId = 6,  Query = "mayer limon",               Priority = 1 },
            new ProductSearchAlias { Id = 22, ProductVarietyId = 6,  Query = "meyer limon",               Priority = 2 },
            new ProductSearchAlias { Id = 23, ProductVarietyId = 6,  Query = "mayer limon kg",            Priority = 3 },
            new ProductSearchAlias { Id = 25, ProductVarietyId = 7,  Query = "kirmizi limon",             Priority = 1 },
            new ProductSearchAlias { Id = 26, ProductVarietyId = 7,  Query = "red lemon",                 Priority = 2 },
            new ProductSearchAlias { Id = 27, ProductVarietyId = 7,  Query = "kirmizi limon fiyat",       Priority = 3 },
            new ProductSearchAlias { Id = 29, ProductVarietyId = 8,  Query = "enterdonat limon",          Priority = 1 },
            new ProductSearchAlias { Id = 30, ProductVarietyId = 8,  Query = "enterdonat limon kg",       Priority = 2 },
            new ProductSearchAlias { Id = 31, ProductVarietyId = 8,  Query = "enternonat limon",          Priority = 3 },
            new ProductSearchAlias { Id = 33, ProductVarietyId = 9,  Query = "lamas limon",               Priority = 1 },
            new ProductSearchAlias { Id = 34, ProductVarietyId = 9,  Query = "klasik limon",              Priority = 2 },
            new ProductSearchAlias { Id = 35, ProductVarietyId = 9,  Query = "lamas limon kg",            Priority = 3 },
            new ProductSearchAlias { Id = 37, ProductVarietyId = 10, Query = "kokulu limon",              Priority = 1 },
            new ProductSearchAlias { Id = 38, ProductVarietyId = 10, Query = "kokulu limon kg",           Priority = 2 },
            new ProductSearchAlias { Id = 39, ProductVarietyId = 10, Query = "limonata limonu",           Priority = 3 },
            new ProductSearchAlias { Id = 41, ProductVarietyId = 11, Query = "grande limon",              Priority = 1 },
            new ProductSearchAlias { Id = 42, ProductVarietyId = 11, Query = "italyan limon",             Priority = 2 },
            new ProductSearchAlias { Id = 43, ProductVarietyId = 11, Query = "grande limon fiyat",        Priority = 3 },
            new ProductSearchAlias { Id = 45, ProductVarietyId = 12, Query = "tatli limon",               Priority = 1 },
            new ProductSearchAlias { Id = 46, ProductVarietyId = 12, Query = "sweet lemon",               Priority = 2 },
            new ProductSearchAlias { Id = 47, ProductVarietyId = 12, Query = "tatli limon fiyat",         Priority = 3 },
            // Lime (IDs 49-64)
            new ProductSearchAlias { Id = 49, ProductVarietyId = 13, Query = "lime",                      Priority = 1 },
            new ProductSearchAlias { Id = 50, ProductVarietyId = 13, Query = "lime kg",                   Priority = 2 },
            new ProductSearchAlias { Id = 51, ProductVarietyId = 13, Query = "yesil limon",               Priority = 3 },
            new ProductSearchAlias { Id = 53, ProductVarietyId = 14, Query = "kaffir lime",               Priority = 1 },
            new ProductSearchAlias { Id = 54, ProductVarietyId = 14, Query = "kaffir lime fiyat",         Priority = 2 },
            new ProductSearchAlias { Id = 55, ProductVarietyId = 14, Query = "kaffir limon",              Priority = 3 },
            new ProductSearchAlias { Id = 57, ProductVarietyId = 15, Query = "tatli lime",                Priority = 1 },
            new ProductSearchAlias { Id = 58, ProductVarietyId = 15, Query = "sweet lime",                Priority = 2 },
            new ProductSearchAlias { Id = 59, ProductVarietyId = 15, Query = "tatli lime fiyat",          Priority = 3 },
            new ProductSearchAlias { Id = 61, ProductVarietyId = 16, Query = "kan lime",                  Priority = 1 },
            new ProductSearchAlias { Id = 62, ProductVarietyId = 16, Query = "blood lime",                Priority = 2 },
            new ProductSearchAlias { Id = 63, ProductVarietyId = 16, Query = "kan lime fiyat",            Priority = 3 },
            // Finger Lime (IDs 65-80)
            new ProductSearchAlias { Id = 65, ProductVarietyId = 17, Query = "verde finger lime",         Priority = 1 },
            new ProductSearchAlias { Id = 66, ProductVarietyId = 17, Query = "finger lime verde",         Priority = 2 },
            new ProductSearchAlias { Id = 67, ProductVarietyId = 17, Query = "havyar limon verde",        Priority = 3 },
            new ProductSearchAlias { Id = 69, ProductVarietyId = 18, Query = "rose finger lime",          Priority = 1 },
            new ProductSearchAlias { Id = 70, ProductVarietyId = 18, Query = "finger lime rose",          Priority = 2 },
            new ProductSearchAlias { Id = 71, ProductVarietyId = 18, Query = "pembe havyar limon",        Priority = 3 },
            new ProductSearchAlias { Id = 73, ProductVarietyId = 19, Query = "sunpearl finger lime",      Priority = 1 },
            new ProductSearchAlias { Id = 74, ProductVarietyId = 19, Query = "finger lime sunpearl",      Priority = 2 },
            new ProductSearchAlias { Id = 75, ProductVarietyId = 19, Query = "sunpearl havyar limon",     Priority = 3 },
            new ProductSearchAlias { Id = 77, ProductVarietyId = 20, Query = "blossom finger lime",       Priority = 1 },
            new ProductSearchAlias { Id = 78, ProductVarietyId = 20, Query = "finger lime blossom",       Priority = 2 },
            new ProductSearchAlias { Id = 79, ProductVarietyId = 20, Query = "blossom havyar limon",      Priority = 3 },
            // Portakal (IDs 81-100)
            new ProductSearchAlias { Id = 81, ProductVarietyId = 21, Query = "washington portakal",       Priority = 1 },
            new ProductSearchAlias { Id = 82, ProductVarietyId = 21, Query = "finike portakal",           Priority = 2 },
            new ProductSearchAlias { Id = 83, ProductVarietyId = 21, Query = "washington portakal kg",    Priority = 3 },
            new ProductSearchAlias { Id = 85, ProductVarietyId = 22, Query = "valensiya portakal",        Priority = 1 },
            new ProductSearchAlias { Id = 86, ProductVarietyId = 22, Query = "valencia portakal",         Priority = 2 },
            new ProductSearchAlias { Id = 87, ProductVarietyId = 22, Query = "valensiya portakal kg",     Priority = 3 },
            new ProductSearchAlias { Id = 89, ProductVarietyId = 23, Query = "mysticrimson portakal",     Priority = 1 },
            new ProductSearchAlias { Id = 90, ProductVarietyId = 23, Query = "mystic crimson orange",     Priority = 2 },
            new ProductSearchAlias { Id = 91, ProductVarietyId = 23, Query = "kirmizi etli portakal",     Priority = 3 },
            new ProductSearchAlias { Id = 93, ProductVarietyId = 24, Query = "blushsweet portakal",       Priority = 1 },
            new ProductSearchAlias { Id = 94, ProductVarietyId = 24, Query = "kan portakali blushsweet",  Priority = 2 },
            new ProductSearchAlias { Id = 95, ProductVarietyId = 24, Query = "pembe portakal",            Priority = 3 },
            new ProductSearchAlias { Id = 97, ProductVarietyId = 25, Query = "seker portakali",           Priority = 1 },
            new ProductSearchAlias { Id = 98, ProductVarietyId = 25, Query = "tatli portakal",            Priority = 2 },
            new ProductSearchAlias { Id = 99, ProductVarietyId = 25, Query = "seker portakal kg",         Priority = 3 },
            // Avokado (IDs 101-120)
            new ProductSearchAlias { Id = 101, ProductVarietyId = 26, Query = "hass avokado",             Priority = 1 },
            new ProductSearchAlias { Id = 102, ProductVarietyId = 26, Query = "puturlu avokado",          Priority = 2 },
            new ProductSearchAlias { Id = 103, ProductVarietyId = 26, Query = "hass avokado kg",          Priority = 3 },
            new ProductSearchAlias { Id = 105, ProductVarietyId = 27, Query = "ettinger avokado",         Priority = 1 },
            new ProductSearchAlias { Id = 106, ProductVarietyId = 27, Query = "ettinger avocado",         Priority = 2 },
            new ProductSearchAlias { Id = 107, ProductVarietyId = 27, Query = "ettinger avokado kg",      Priority = 3 },
            new ProductSearchAlias { Id = 109, ProductVarietyId = 28, Query = "bacon avokado",            Priority = 1 },
            new ProductSearchAlias { Id = 110, ProductVarietyId = 28, Query = "bacon avocado",            Priority = 2 },
            new ProductSearchAlias { Id = 111, ProductVarietyId = 28, Query = "bacon avokado kg",         Priority = 3 },
            new ProductSearchAlias { Id = 113, ProductVarietyId = 29, Query = "fuerte avokado",           Priority = 1 },
            new ProductSearchAlias { Id = 114, ProductVarietyId = 29, Query = "fuerte avocado",           Priority = 2 },
            new ProductSearchAlias { Id = 115, ProductVarietyId = 29, Query = "fuerte avokado kg",        Priority = 3 },
            new ProductSearchAlias { Id = 117, ProductVarietyId = 30, Query = "cliffton avokado",         Priority = 1 },
            new ProductSearchAlias { Id = 118, ProductVarietyId = 30, Query = "clifton avokado",          Priority = 2 },
            new ProductSearchAlias { Id = 119, ProductVarietyId = 30, Query = "cliffton avocado",         Priority = 3 },
            // Diğer Ürünler (IDs 121-140)
            new ProductSearchAlias { Id = 121, ProductVarietyId = 31, Query = "bergamot",                 Priority = 1 },
            new ProductSearchAlias { Id = 122, ProductVarietyId = 31, Query = "bergamot kg",              Priority = 2 },
            new ProductSearchAlias { Id = 123, ProductVarietyId = 31, Query = "bergamot fiyat",           Priority = 3 },
            new ProductSearchAlias { Id = 125, ProductVarietyId = 32, Query = "kumkuat",                  Priority = 1 },
            new ProductSearchAlias { Id = 126, ProductVarietyId = 32, Query = "kamkat",                   Priority = 2 },
            new ProductSearchAlias { Id = 127, ProductVarietyId = 32, Query = "kumkuat kg",               Priority = 3 },
            new ProductSearchAlias { Id = 129, ProductVarietyId = 33, Query = "sadok",                    Priority = 1 },
            new ProductSearchAlias { Id = 130, ProductVarietyId = 33, Query = "pomelo greyfurt",          Priority = 2 },
            new ProductSearchAlias { Id = 131, ProductVarietyId = 33, Query = "sadok fiyat",              Priority = 3 },
            new ProductSearchAlias { Id = 133, ProductVarietyId = 34, Query = "beyaz greyfurt",           Priority = 1 },
            new ProductSearchAlias { Id = 134, ProductVarietyId = 34, Query = "white grapefruit",         Priority = 2 },
            new ProductSearchAlias { Id = 135, ProductVarietyId = 34, Query = "beyaz greyfurt kg",        Priority = 3 },
            new ProductSearchAlias { Id = 137, ProductVarietyId = 35, Query = "kirmizi greyfurt",         Priority = 1 },
            new ProductSearchAlias { Id = 138, ProductVarietyId = 35, Query = "red grapefruit",           Priority = 2 },
            new ProductSearchAlias { Id = 139, ProductVarietyId = 35, Query = "kirmizi greyfurt kg",      Priority = 3 },
            // Yeni çeşitler (IDs 141+, pattern: varietyId*4-3)
            new ProductSearchAlias { Id = 141, ProductVarietyId = 36, Query = "limekuat",                 Priority = 1 },
            new ProductSearchAlias { Id = 142, ProductVarietyId = 36, Query = "limequat",                 Priority = 2 },
            new ProductSearchAlias { Id = 143, ProductVarietyId = 36, Query = "limekuat kg",              Priority = 3 },
            new ProductSearchAlias { Id = 145, ProductVarietyId = 37, Query = "turunc",                   Priority = 1 },
            new ProductSearchAlias { Id = 146, ProductVarietyId = 37, Query = "turunc portakal",          Priority = 2 },
            new ProductSearchAlias { Id = 147, ProductVarietyId = 37, Query = "aci portakal",             Priority = 3 },
            new ProductSearchAlias { Id = 149, ProductVarietyId = 38, Query = "hicaz nar",                Priority = 1 },
            new ProductSearchAlias { Id = 150, ProductVarietyId = 38, Query = "hicaz nar kg",             Priority = 2 },
            new ProductSearchAlias { Id = 151, ProductVarietyId = 38, Query = "nar kg",                   Priority = 3 },
            new ProductSearchAlias { Id = 153, ProductVarietyId = 39, Query = "hicaz 9 eylul nar",       Priority = 1 },
            new ProductSearchAlias { Id = 154, ProductVarietyId = 39, Query = "erkenci nar",              Priority = 2 },
            new ProductSearchAlias { Id = 157, ProductVarietyId = 40, Query = "ejder meyvesi",            Priority = 1 },
            new ProductSearchAlias { Id = 158, ProductVarietyId = 40, Query = "kirmizi ejder meyvesi",    Priority = 2 },
            new ProductSearchAlias { Id = 159, ProductVarietyId = 40, Query = "dragon fruit",             Priority = 3 },
            new ProductSearchAlias { Id = 161, ProductVarietyId = 41, Query = "sari ejder meyvesi",       Priority = 1 },
            new ProductSearchAlias { Id = 162, ProductVarietyId = 41, Query = "yellow dragon fruit",      Priority = 2 },
            new ProductSearchAlias { Id = 165, ProductVarietyId = 42, Query = "carkifelek",               Priority = 1 },
            new ProductSearchAlias { Id = 166, ProductVarietyId = 42, Query = "passion fruit",            Priority = 2 },
            new ProductSearchAlias { Id = 167, ProductVarietyId = 42, Query = "carkifelek kg",            Priority = 3 },
            new ProductSearchAlias { Id = 169, ProductVarietyId = 43, Query = "sari carkifelek",          Priority = 1 },
            new ProductSearchAlias { Id = 170, ProductVarietyId = 43, Query = "yellow passion fruit",     Priority = 2 },
            new ProductSearchAlias { Id = 173, ProductVarietyId = 44, Query = "mango",                    Priority = 1 },
            new ProductSearchAlias { Id = 174, ProductVarietyId = 44, Query = "kent mango",               Priority = 2 },
            new ProductSearchAlias { Id = 175, ProductVarietyId = 44, Query = "mango kg",                 Priority = 3 },
            new ProductSearchAlias { Id = 177, ProductVarietyId = 45, Query = "tommy atkins mango",       Priority = 1 },
            new ProductSearchAlias { Id = 178, ProductVarietyId = 45, Query = "tommy mango kg",           Priority = 2 },
            new ProductSearchAlias { Id = 181, ProductVarietyId = 46, Query = "keitt mango",              Priority = 1 },
            new ProductSearchAlias { Id = 182, ProductVarietyId = 46, Query = "yesil mango",              Priority = 2 },
            new ProductSearchAlias { Id = 185, ProductVarietyId = 47, Query = "salkimli domates",         Priority = 1 },
            new ProductSearchAlias { Id = 186, ProductVarietyId = 47, Query = "domates kg",               Priority = 2 },
            new ProductSearchAlias { Id = 189, ProductVarietyId = 48, Query = "san marzano domates",      Priority = 1 },
            new ProductSearchAlias { Id = 190, ProductVarietyId = 48, Query = "san marzano kg",           Priority = 2 },
            new ProductSearchAlias { Id = 193, ProductVarietyId = 49, Query = "cherry domates",           Priority = 1 },
            new ProductSearchAlias { Id = 194, ProductVarietyId = 49, Query = "kiraz domates",            Priority = 2 },
            new ProductSearchAlias { Id = 197, ProductVarietyId = 50, Query = "limon otu",                Priority = 1 },
            new ProductSearchAlias { Id = 198, ProductVarietyId = 50, Query = "lemon grass",              Priority = 2 },
            new ProductSearchAlias { Id = 199, ProductVarietyId = 50, Query = "limon otu demet",          Priority = 3 },
            new ProductSearchAlias { Id = 201, ProductVarietyId = 51, Query = "kurutulmus limon otu",     Priority = 1 },
            new ProductSearchAlias { Id = 202, ProductVarietyId = 51, Query = "dried lemon grass",        Priority = 2 },
            // Reçel & Marmelat
            new ProductSearchAlias { Id = 205, ProductVarietyId = 52, Query = "portakal receli",          Priority = 1 },
            new ProductSearchAlias { Id = 206, ProductVarietyId = 52, Query = "portakal kabugu receli",   Priority = 2 },
            new ProductSearchAlias { Id = 209, ProductVarietyId = 53, Query = "turunc receli",            Priority = 1 },
            new ProductSearchAlias { Id = 210, ProductVarietyId = 53, Query = "turunc kabugu receli",     Priority = 2 },
            new ProductSearchAlias { Id = 213, ProductVarietyId = 54, Query = "bergamot receli",          Priority = 1 },
            new ProductSearchAlias { Id = 214, ProductVarietyId = 54, Query = "bergamot kabugu receli",   Priority = 2 },
            new ProductSearchAlias { Id = 217, ProductVarietyId = 55, Query = "limon receli",             Priority = 1 },
            new ProductSearchAlias { Id = 218, ProductVarietyId = 55, Query = "limon kabugu receli",      Priority = 2 },
            new ProductSearchAlias { Id = 221, ProductVarietyId = 56, Query = "kumkuat receli",           Priority = 1 },
            new ProductSearchAlias { Id = 225, ProductVarietyId = 57, Query = "nar receli",               Priority = 1 },
            new ProductSearchAlias { Id = 229, ProductVarietyId = 58, Query = "mandalina marmelati",      Priority = 1 },
            new ProductSearchAlias { Id = 230, ProductVarietyId = 58, Query = "mandalina receli",         Priority = 2 },
            new ProductSearchAlias { Id = 233, ProductVarietyId = 59, Query = "erik marmelati",           Priority = 1 },
            new ProductSearchAlias { Id = 234, ProductVarietyId = 59, Query = "carkifelek marmelati",     Priority = 2 },
            new ProductSearchAlias { Id = 235, ProductVarietyId = 59, Query = "armut marmelati",          Priority = 3 },
            // Nar Ekşisi
            new ProductSearchAlias { Id = 237, ProductVarietyId = 60, Query = "nar eksisi",               Priority = 1 },
            new ProductSearchAlias { Id = 238, ProductVarietyId = 60, Query = "pomegranate molasses",     Priority = 2 },
            new ProductSearchAlias { Id = 239, ProductVarietyId = 60, Query = "dogal nar eksisi",         Priority = 3 },
            // Kurutulmuş Ürünler
            new ProductSearchAlias { Id = 241, ProductVarietyId = 61, Query = "portakal kurusu",          Priority = 1 },
            new ProductSearchAlias { Id = 242, ProductVarietyId = 61, Query = "mandalina cipsi",          Priority = 2 },
            new ProductSearchAlias { Id = 243, ProductVarietyId = 61, Query = "limon kurusu",             Priority = 3 },
            new ProductSearchAlias { Id = 245, ProductVarietyId = 62, Query = "kan portakal kurusu",      Priority = 1 },
            new ProductSearchAlias { Id = 249, ProductVarietyId = 63, Query = "nar kurusu",               Priority = 1 },
            new ProductSearchAlias { Id = 253, ProductVarietyId = 64, Query = "kaffir lime yapragi",      Priority = 1 },
            new ProductSearchAlias { Id = 254, ProductVarietyId = 64, Query = "kurutulmus lime yaprak",   Priority = 2 },
            new ProductSearchAlias { Id = 257, ProductVarietyId = 65, Query = "lime kurusu",              Priority = 1 },
            new ProductSearchAlias { Id = 258, ProductVarietyId = 65, Query = "freeze dry lime",          Priority = 2 },
            // Meyve Suyu
            new ProductSearchAlias { Id = 261, ProductVarietyId = 66, Query = "nar suyu",                 Priority = 1 },
            new ProductSearchAlias { Id = 262, ProductVarietyId = 66, Query = "pomegranate juice",        Priority = 2 },
            new ProductSearchAlias { Id = 265, ProductVarietyId = 67, Query = "limon suyu",               Priority = 1 },
            new ProductSearchAlias { Id = 266, ProductVarietyId = 67, Query = "lemon juice",              Priority = 2 },
        ];
    }

    private static ProductSearchAlias[] BuildAliases(int startId, int varietyId, params string[] queries)
    {
        return queries
            .Select((query, index) => new ProductSearchAlias
            {
                Id = startId + index,
                ProductVarietyId = varietyId,
                Query = query,
                Priority = index + 1
            })
            .ToArray();
    }
}
