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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasIndex(product => product.Name)
            .IsUnique();

        modelBuilder.Entity<Market>()
            .HasIndex(market => market.Name)
            .IsUnique();

        modelBuilder.Entity<PriceRecord>()
            .HasIndex(record => new { record.ProductId, record.MarketId, record.CheckedAt });

        modelBuilder.Entity<ProductVariety>()
            .HasIndex(variety => new { variety.ProductId, variety.Name })
            .IsUnique();

        modelBuilder.Entity<ProductSearchAlias>()
            .HasIndex(alias => new { alias.ProductVarietyId, alias.Query })
            .IsUnique();

        modelBuilder.Entity<Market>().HasData(
            new Market { Id = 1, Name = "Migros", BaseUrl = "https://www.migros.com.tr", SearchUrlTemplate = "https://www.migros.com.tr/arama?q={0}" },
            new Market { Id = 2, Name = "A101", BaseUrl = "https://www.a101.com.tr", SearchUrlTemplate = "https://www.a101.com.tr/arama/?search_text={0}" },
            new Market { Id = 3, Name = "CarrefourSA", BaseUrl = "https://www.carrefoursa.com", SearchUrlTemplate = "https://www.carrefoursa.com/search/?text={0}" },
            new Market { Id = 4, Name = "Sok", BaseUrl = "https://www.sokmarket.com.tr", SearchUrlTemplate = "https://www.sokmarket.com.tr/arama?q={0}" },
            new Market { Id = 5, Name = "BIM", BaseUrl = "https://www.bim.com.tr", SearchUrlTemplate = "https://www.bim.com.tr" },
            new Market { Id = 6, Name = "Web Arama", BaseUrl = "https://search.brave.com", SearchUrlTemplate = null });

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Mandalina", Category = "Narenciye", Unit = "Kg", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 2, Name = "Limon", Category = "Narenciye", Unit = "Kg", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 3, Name = "Lime", Category = "Narenciye", Unit = "Kg", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 4, Name = "Finger Lime", Category = "Narenciye", Unit = "Kg", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 5, Name = "Portakal", Category = "Narenciye", Unit = "Kg", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 6, Name = "Avokado", Category = "Tropikal", Unit = "Kg", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Product { Id = 7, Name = "Diger Urunler", Category = "Narenciye", Unit = "Kg", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) });

        modelBuilder.Entity<ProductVariety>().HasData(
            new ProductVariety { Id = 1, ProductId = 1, Name = "OKITSU", HarvestPeriod = "Eylul sonu", Notes = "Cekirdeksiz; ilk donem eksimsi, sonra tatlilasir; ilk hasatta yesil." },
            new ProductVariety { Id = 2, ProductId = 1, Name = "SATSUMA", HarvestPeriod = "Ekim sonu - Kasim", Notes = "Cekirdeksiz; sulu ve tatli." },
            new ProductVariety { Id = 3, ProductId = 1, Name = "KLEMANTIN", HarvestPeriod = "Kasim sonu", Notes = "Cekirdekli; yogun aromali; kisa hasat donemi." },
            new ProductVariety { Id = 4, ProductId = 1, Name = "ORRI", HarvestPeriod = "Ocak sonu", Notes = "Cok tatli; sulu ve aromatik; az cekirdek gorulebilir." },
            new ProductVariety { Id = 5, ProductId = 1, Name = "MURCOTT", HarvestPeriod = "Ocak sonu", Notes = "Cekirdekli; yogun tatli, hafif eksi; sert ve parlak kabuklu." },
            new ProductVariety { Id = 6, ProductId = 2, Name = "MAYER", HarvestPeriod = "Eylul basi", Notes = "Mandalina-limon aromasi; az asitli, bol sulu." },
            new ProductVariety { Id = 7, ProductId = 2, Name = "KIRMIZI LIMON", Notes = "Portakal ve limon karisimi aroma; cekirdekli; hafif eksi." },
            new ProductVariety { Id = 8, ProductId = 2, Name = "ENTERDONAT", HarvestPeriod = "Eylul sonu", Notes = "En eksi limon; kalin kabuklu." },
            new ProductVariety { Id = 9, ProductId = 2, Name = "LAMAS", HarvestPeriod = "Aralik - Nisan", Notes = "Klasik limon; tam limon aromasi; cay icin ideal." },
            new ProductVariety { Id = 10, ProductId = 2, Name = "KOKULU LIMON", HarvestPeriod = "Mart basi", Notes = "Limonata icin ideal; cok sulu." },
            new ProductVariety { Id = 11, ProductId = 2, Name = "GRANDE", Notes = "Italyan kokenli; kalin kabuklu; recel ve iceceklerde kullanilir." },
            new ProductVariety { Id = 12, ProductId = 2, Name = "TATLI LIMON", Notes = "Eksilik yoktur; limonata icin uygundur." },
            new ProductVariety { Id = 13, ProductId = 3, Name = "LIME", Notes = "Ince kabuklu; sulu; aromasi yuksek." },
            new ProductVariety { Id = 14, ProductId = 3, Name = "KAFFIR LIME", Notes = "Cok yogun aroma; az sulu; yemek ve soslarda kullanilir." },
            new ProductVariety { Id = 15, ProductId = 3, Name = "TATLI LIME", Notes = "Mandalinaya benzer; eksi degil, tatlidir." },
            new ProductVariety { Id = 16, ProductId = 3, Name = "KAN LIME", Notes = "Kirmizi tonlu; nadir cesit." },
            new ProductVariety { Id = 17, ProductId = 4, Name = "VERDE", Notes = "Finger lime; keskin aroma; asidi yuksek; parlak yesil taneler." },
            new ProductVariety { Id = 18, ProductId = 4, Name = "ROSE", Notes = "Finger lime; pembe taneli; asidi dengeli; daha sulu." },
            new ProductVariety { Id = 19, ProductId = 4, Name = "SUNPEARL", Notes = "Finger lime; sari-yesil; mat ve sert taneli." },
            new ProductVariety { Id = 20, ProductId = 4, Name = "BLOSSOM", Notes = "Finger lime; uzun ve iri taneli." },
            new ProductVariety { Id = 21, ProductId = 5, Name = "WASHINGTON", HarvestPeriod = "Kasim basi", Notes = "Finike portakali; cok tatli; cekirdeksiz." },
            new ProductVariety { Id = 22, ProductId = 5, Name = "VALENSIYA", HarvestPeriod = "Nisan - Eylul", Notes = "Cekirdekli; sikmalik ve yemelik." },
            new ProductVariety { Id = 23, ProductId = 5, Name = "MYSTICRIMSON", Notes = "Kirmizi etli; hafif eksimsi; ince kabuklu." },
            new ProductVariety { Id = 24, ProductId = 5, Name = "BLUSHSWEET", Notes = "Kan portakali; pembe ic renk; tatli; ince kabuklu." },
            new ProductVariety { Id = 25, ProductId = 5, Name = "SEKER PORTAKALI", Notes = "Cok tatli; asidi dusuk." },
            new ProductVariety { Id = 26, ProductId = 6, Name = "HASS", Notes = "Puturlu avokado; en yuksek yag orani; guclu aroma." },
            new ProductVariety { Id = 27, ProductId = 6, Name = "ETTINGER", Notes = "Armut sekilli; parlak kabuklu." },
            new ProductVariety { Id = 28, ProductId = 6, Name = "BACON", Notes = "Ince kabuklu; hafif lezzetli." },
            new ProductVariety { Id = 29, ProductId = 6, Name = "FUERTE", Notes = "Hafif puturlu; ince kabuklu." },
            new ProductVariety { Id = 30, ProductId = 6, Name = "CLIFFTON", Notes = "Yag orani dusuk; sulu yapi." },
            new ProductVariety { Id = 31, ProductId = 7, Name = "BERGAMOT", Notes = "Meyvesi yenmez; kabugu recel ve cayda kullanilir." },
            new ProductVariety { Id = 32, ProductId = 7, Name = "KUMKUAT", Notes = "Kabuguyla yenir; disi tatli, ici eksi." },
            new ProductVariety { Id = 33, ProductId = 7, Name = "SADOK", Notes = "Pomelo + greyfurt aromasi; kalin kabuklu." },
            new ProductVariety { Id = 34, ProductId = 7, Name = "BEYAZ GREYFURT", Notes = "Beyaz ic ve dis renk." },
            new ProductVariety { Id = 35, ProductId = 7, Name = "KIRMIZI GREYFURT", Notes = "Kirmizi ic renk; daha yumusak tat." });

        modelBuilder.Entity<ProductSearchAlias>().HasData(BuildAllAliases());
    }

    private static ProductSearchAlias[] BuildAllAliases()
    {
        return
        [
            .. BuildAliases(1, 1, "okitsu mandalina", "okitsu mandalina kg", "okitsu fiyat", "okitsu fidan meyve"),
            .. BuildAliases(5, 2, "satsuma mandalina", "satsuma mandalina kg", "satsuma fiyat"),
            .. BuildAliases(9, 3, "klemantin mandalina", "clementine mandalina", "klemantin fiyat"),
            .. BuildAliases(13, 4, "orri mandalina", "orri mandarin", "orri fiyat"),
            .. BuildAliases(17, 5, "murcott mandalina", "murcott mandarin", "murcott fiyat"),
            .. BuildAliases(21, 6, "mayer limon", "meyer limon", "mayer limon kg"),
            .. BuildAliases(25, 7, "kirmizi limon", "red lemon", "kirmizi limon fiyat"),
            .. BuildAliases(29, 8, "enterdonat limon", "enterdonat limon kg", "enternonat limon"),
            .. BuildAliases(33, 9, "lamas limon", "klasik limon", "lamas limon kg"),
            .. BuildAliases(37, 10, "kokulu limon", "kokulu limon kg", "limonata limonu"),
            .. BuildAliases(41, 11, "grande limon", "italyan limon", "grande limon fiyat"),
            .. BuildAliases(45, 12, "tatli limon", "sweet lemon", "tatli limon fiyat"),
            .. BuildAliases(49, 13, "lime", "lime kg", "yesil limon"),
            .. BuildAliases(53, 14, "kaffir lime", "kaffir lime fiyat", "kaffir limon"),
            .. BuildAliases(57, 15, "tatli lime", "sweet lime", "tatli lime fiyat"),
            .. BuildAliases(61, 16, "kan lime", "blood lime", "kan lime fiyat"),
            .. BuildAliases(65, 17, "verde finger lime", "finger lime verde", "havyar limon verde"),
            .. BuildAliases(69, 18, "rose finger lime", "finger lime rose", "pembe havyar limon"),
            .. BuildAliases(73, 19, "sunpearl finger lime", "finger lime sunpearl", "sunpearl havyar limon"),
            .. BuildAliases(77, 20, "blossom finger lime", "finger lime blossom", "blossom havyar limon"),
            .. BuildAliases(81, 21, "washington portakal", "finike portakal", "washington portakal kg"),
            .. BuildAliases(85, 22, "valensiya portakal", "valencia portakal", "valensiya portakal kg"),
            .. BuildAliases(89, 23, "mysticrimson portakal", "mystic crimson orange", "kirmizi etli portakal"),
            .. BuildAliases(93, 24, "blushsweet portakal", "kan portakali blushsweet", "pembe portakal"),
            .. BuildAliases(97, 25, "seker portakali", "tatli portakal", "seker portakal kg"),
            .. BuildAliases(101, 26, "hass avokado", "puturlu avokado", "hass avokado kg"),
            .. BuildAliases(105, 27, "ettinger avokado", "ettinger avocado", "ettinger avokado kg"),
            .. BuildAliases(109, 28, "bacon avokado", "bacon avocado", "bacon avokado kg"),
            .. BuildAliases(113, 29, "fuerte avokado", "fuerte avocado", "fuerte avokado kg"),
            .. BuildAliases(117, 30, "cliffton avokado", "clifton avokado", "cliffton avocado"),
            .. BuildAliases(121, 31, "bergamot", "bergamot kg", "bergamot fiyat"),
            .. BuildAliases(125, 32, "kumkuat", "kamkat", "kumkuat kg"),
            .. BuildAliases(129, 33, "sadok", "pomelo greyfurt", "sadok fiyat"),
            .. BuildAliases(133, 34, "beyaz greyfurt", "white grapefruit", "beyaz greyfurt kg"),
            .. BuildAliases(137, 35, "kirmizi greyfurt", "red grapefruit", "kirmizi greyfurt kg")
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
