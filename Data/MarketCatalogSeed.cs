using System.Text;
using BahceFiyatTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Data;

internal static class MarketCatalogSeed
{
    public static IReadOnlyList<MarketSeed> All { get; } =
    [
        new(@"Migros", @"https://www.migros.com.tr", @"https://www.migros.com.tr/rest/products/search?q={0}&sayfa=0&sira=ONERILENLER&webSubdomain=www", true),
        new(@"A101", @"https://www.a101.com.tr", @"https://www.a101.com.tr/arama/?search_text={0}", true),
        new(@"CarrefourSA", @"https://www.carrefoursa.com", @"https://www.carrefoursa.com/search/?text={0}", true),
        new(@"Sok", @"https://www.sokmarket.com.tr", @"https://www.sokmarket.com.tr/arama?q={0}", true),
        new(@"BIM", @"https://www.bim.com.tr", null, false),
        new(@"Web Arama", @"https://search.brave.com", null, false),
        new(@"Macrocenter", @"https://www.macrocenter.com.tr", @"https://www.macrocenter.com.tr/arama?q={0}", true),
        new(@"File Market", @"https://www.filemarket.com.tr", @"https://www.filemarket.com.tr/arama?q={0}", true),
        new(@"CarrefourSA Gurme", @"https://www.carrefoursa.com", @"https://www.carrefoursa.com/search/?text={0}", true),
        new(@"Getir Büyük", @"https://getir.com", null, false),
        new(@"Trendyol Market", @"https://www.trendyol.com", null, false),
        new(@"Yemeksepeti Market", @"https://www.yemeksepeti.com", null, false),
        new(@"Hepsiburada Market", @"https://www.hepsiburada.com", null, false),
        new(@"Afta Market", null, null, false),
        new(@"Alta Natural", null, null, false),
        new(@"AltaNaturel", null, null, false),
        new(@"Anamur Bahçesi", @"https://www.anamurbahcesi.com", @"https://www.anamurbahcesi.com/?s={0}", true),
        new(@"Anamurbahçesi", null, null, false),
        new(@"Anamurdalından", null, null, false),
        new(@"Ancora", null, null, false),
        new(@"Antalya Reçelcisi", null, null, false),
        new(@"Arden", null, null, false),
        new(@"Arifoğlu", null, null, false),
        new(@"Avokadocu Ayşe", null, null, false),
        new(@"AvokadocuAyşe", null, null, false),
        new(@"Avokadolu", null, null, false),
        new(@"Babamın Bahçesi", null, null, false),
        new(@"Babamın Bahçesi (HB)", null, null, false),
        new(@"Ben Organik 250 Ml (Fresh Selective)", null, null, false),
        new(@"Beyorganik", null, null, false),
        new(@"Birfincan", null, null, false),
        new(@"Bodrum Yadigarı", null, null, false),
        new(@"Büyükannem", null, null, false),
        new(@"Carrefour", @"https://www.carrefoursa.com", @"https://www.carrefoursa.com/search/?text={0}", false),
        new(@"Carrefoursa Naren 250 Ml", @"https://www.carrefoursa.com", @"https://www.carrefoursa.com/search/?text={0}", false),
        new(@"Dalından Lezzet", null, null, false),
        new(@"Damlıca Çiftliği", null, null, false),
        new(@"DatçaMuratÇiftliği", null, null, false),
        new(@"Değirmencidede", null, null, false),
        new(@"Doğalyaşam", null, null, false),
        new(@"Dünyanınmeyvesi", null, null, false),
        new(@"Ege Pazarından", null, null, false),
        new(@"Ege'ye Dönüş", null, null, false),
        new(@"Egepazarından", null, null, false),
        new(@"Ejdermeyvesi", null, null, false),
        new(@"Elite Organic 200 ml 12'li", null, null, false),
        new(@"Elite Organic 200 ml 4'lü", null, null, false),
        new(@"Enmanav", null, null, false),
        new(@"Entazem", @"https://www.entazem.com", null, false),
        new(@"Eski Tadında", null, null, false),
        new(@"eskitadinda", null, null, false),
        new(@"Eskitadında", null, null, false),
        new(@"Etrog(El Camino-Mandalina)", null, null, false),
        new(@"Etrog(Hagar-Kumkuat)", null, null, false),
        new(@"Etrog(Miryam-Turunç)", null, null, false),
        new(@"Etrog(Vortan Garmir-3 çeşit portakal)", null, null, false),
        new(@"f(x) food", null, null, false),
        new(@"Fresh Selective", null, null, false),
        new(@"Freshselective", null, null, false),
        new(@"Gurmeköy", null, null, false),
        new(@"Gurmenin yeri", null, null, false),
        new(@"Gülen Bahçe", null, null, false),
        new(@"Gülenbahçe", null, null, false),
        new(@"Gürmar", @"https://www.gurmar.com.tr", @"https://www.gurmar.com.tr/?s={0}", true),
        new(@"Hammaddelergupguru", null, null, false),
        new(@"Hatayköy Yöresel", null, null, false),
        new(@"Hediyelik Bahçem", null, null, false),
        new(@"Hediyelikbahçem", @"https://www.hediyelikbahcem.com", @"https://www.hediyelikbahcem.com/?s={0}", true),
        new(@"Hepsi Bahçemden", null, null, false),
        new(@"Hepsiburada Sebzemeyvedünyası", null, null, false),
        new(@"İkiçay", null, null, false),
        new(@"İkiçay (trendyol)", null, null, false),
        new(@"İstegelsin", null, null, false),
        new(@"Kaptan Tarım", null, null, false),
        new(@"Kurual", null, null, false),
        new(@"Kuzey Tropik(Hepsiburada)", null, null, false),
        new(@"Köyceğiz Yöresel(Pazarama)", null, null, false),
        new(@"Lokmacıana", null, null, false),
        new(@"Macro", @"https://www.macrocenter.com.tr", @"https://www.macrocenter.com.tr/arama?q={0}", false),
        new(@"Macro Exotic Nar Suyu 750 Cc", null, null, false),
        new(@"Malatya Pazarı", null, null, false),
        new(@"Marketpaketi(Ancora)", null, null, false),
        new(@"Metro", null, null, false),
        new(@"Meyvebahçeniz", null, null, false),
        new(@"Migros Sanal Market", @"https://www.migros.com.tr", @"https://www.migros.com.tr/arama?q={0}", false),
        new(@"Migros Sanal Market(Yenigün Limon Kabuğu)", @"https://www.migros.com.tr", @"https://www.migros.com.tr/arama?q={0}", false),
        new(@"Migros Sanal Market(Yenigün Portakal kabuğu)", @"https://www.migros.com.tr", @"https://www.migros.com.tr/arama?q={0}", false),
        new(@"Migroshemen", @"https://www.migros.com.tr", @"https://www.migros.com.tr/arama?q={0}", false),
        new(@"Migrossanalmarket", @"https://www.migros.com.tr", @"https://www.migros.com.tr/arama?q={0}", false),
        new(@"Musko", null, null, false),
        new(@"Mutlu Sebzeler", null, null, false),
        new(@"N11 (Doğalkasa)", null, null, false),
        new(@"Naciye Hanımın Çiftliği", @"https://www.naciyehanimciftligi.com", @"https://www.naciyehanimciftligi.com/?s={0}", true),
        new(@"NaciyeHanımÇiftliği", null, null, false),
        new(@"NaciyeHanımınÇiftliği", null, null, false),
        new(@"Nartalya 330 ml", null, null, false),
        new(@"Nartalya 950 Ml", null, null, false),
        new(@"Nuhunambarı", null, null, false),
        new(@"Nuri Bey", null, null, false),
        new(@"Nuribey", null, null, false),
        new(@"Nuribey Çiftliği", null, null, false),
        new(@"Nuribeyçiftliği", null, null, false),
        new(@"Onadeğer", null, null, false),
        new(@"Organik Limon", null, null, false),
        new(@"Organikgiller", null, null, false),
        new(@"Otçu", null, null, false),
        new(@"Panayır Gourmet", null, null, false),
        new(@"Panayırgourmet", null, null, false),
        new(@"Pol's Gourme", null, null, false),
        new(@"Pol's Gourme(chia Tohumlu)", null, null, false),
        new(@"Portakalbahcem", @"https://www.portakalbahcem.com", @"https://www.portakalbahcem.com/?s={0}", true),
        new(@"Portakalbahcem 6 adet 250 Ml", null, null, false),
        new(@"portakalbahcem(kırmızı)", null, null, false),
        new(@"portakalbahcem.com", null, null, false),
        new(@"Portakalbahçem", null, null, false),
        new(@"PTTAVM AOÇ 200 Ml", null, null, false),
        new(@"Sarıyer Market", @"https://www.sariyermarket.com", null, false),
        new(@"Sarıyermarket", null, null, false),
        new(@"Sebze Meyve Dünyası", null, null, false),
        new(@"Sebzemeyvedunyası", null, null, false),
        new(@"Sebzemeyvedünyası", null, null, false),
        new(@"Sebzemeyvedünyası (Verita)", null, null, false),
        new(@"Sebzemeyvedünyası(HB)", null, null, false),
        new(@"Sofya'nınarkabahçesi", null, null, false),
        new(@"sokmarket", @"https://www.sokmarket.com.tr", @"https://www.sokmarket.com.tr/arama?q={0}", false),
        new(@"Söyle Yerinden", null, null, false),
        new(@"Söyleyerinden", @"https://www.soyleyerinden.com", @"https://www.soyleyerinden.com/?s={0}", true),
        new(@"Tarım kredi Market 946 ML", null, null, false),
        new(@"Taze Direkt", @"https://www.tazedirekt.com", null, false),
        new(@"Taze Dükkan", @"https://www.tazedukkan.com.tr", @"https://www.tazedukkan.com.tr/?s={0}", true),
        new(@"Taze Dükkan(Şeker Hanım Marmelat)", null, null, false),
        new(@"Taze Dükkan(Şeker Hanım)", null, null, false),
        new(@"Taze Masa", null, null, false),
        new(@"Tazedirekt", null, null, false),
        new(@"Tazedükkan", null, null, false),
        new(@"Tazegel", null, null, false),
        new(@"Topla Sepeti", null, null, false),
        new(@"Tos Grup", null, null, false),
        new(@"Tropik sepeti", null, null, false),
        new(@"Tropikal Türkiye", null, null, false),
        new(@"Tropiksepeti", null, null, false),
        new(@"Turkuazköy", @"https://www.turkuazkoy.com", @"https://www.turkuazkoy.com/?s={0}", true),
        new(@"Turuncubag", null, null, false),
        new(@"Turuncubağ", null, null, false),
        new(@"Verita", null, null, false),
        new(@"Vitaly(HB)", null, null, false),
        new(@"Yenigün", null, null, false),
        new(@"Yeşil Limon", @"https://www.yesillimon.com", @"https://www.yesillimon.com/?s={0}", true),
        new(@"yeşillimon", null, null, false),
        new(@"Yöreselyaşam", null, null, false),
        new(@"Yörükana", null, null, false),
        new(@"Çamlık Gıda", null, null, false),
        new(@"Çiftçideneve", null, null, false),
        new(@"Çitlekçi", null, null, false),
        new(@"Özgürleblebi", null, null, false),
        new(@"Ümit Kuruyemiş", null, null, false),
        new(@"Ünal Kuruyemiş", null, null, false),
        new(@"Şeker Hanım", null, null, false),
        new(@"Şeker Hanım (Marmelat)", null, null, false),
        new(@"Şeker Hanım(Mürdüm)", null, null, false),
        new(@"Şok", null, null, false),
    ];

    public static Market[] ToSeedEntities()
    {
        return All
            .Select((market, index) => new Market
            {
                Id = index + 1,
                Name = market.Name,
                BaseUrl = market.BaseUrl,
                SearchUrlTemplate = market.SearchUrlTemplate,
                IsActive = market.IsActive
            })
            .ToArray();
    }
}

internal static class MarketCatalogSeeder
{
    public static async Task EnsureSeededAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var existingMarkets = await dbContext.Markets.ToListAsync(cancellationToken);

        foreach (var item in MarketCatalogSeed.All)
        {
            var existing = existingMarkets.FirstOrDefault(market =>
                string.Equals(NormalizeMarketName(market.Name), NormalizeMarketName(item.Name), StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                dbContext.Markets.Add(new Market
                {
                    Name = item.Name,
                    BaseUrl = item.BaseUrl,
                    SearchUrlTemplate = item.SearchUrlTemplate,
                    IsActive = item.IsActive
                });

                continue;
            }

            existing.Name = item.Name;
            existing.BaseUrl = item.BaseUrl;
            existing.SearchUrlTemplate = item.SearchUrlTemplate;
            existing.IsActive = item.IsActive;
        }

        await RemoveBadHistoricalMatchesAsync(dbContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }


    private static async Task RemoveBadHistoricalMatchesAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var records = await dbContext.PriceRecords
            .Include(record => record.Product)
            .ToListAsync(cancellationToken);

        var badRecords = records
            .Where(record =>
                IsFreshProduceName(record.Product.Name) &&
                (record.PricePerKg < 10 ||
                 ContainsBadOfferText(record.MatchedTitle) ||
                 ContainsBadOfferText(record.ImageUrl) ||
                 ContainsBadOfferText(record.SourceUrl)))
            .ToList();

        if (badRecords.Count > 0)
        {
            dbContext.PriceRecords.RemoveRange(badRecords);
        }
    }

    private static bool IsFreshProduceName(string value)
    {
        return ContainsAnyText(value,
            "Mandalina",
            "Limon",
            "Lime",
            "Finger Lime",
            "Portakal",
            "Avokado",
            "Greyfurt",
            "Kumkuat",
            "Bergamot");
    }

    private static bool ContainsBadOfferText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return ContainsAnyText(value,
            "Fanta",
            "Cool Lime",
            "Cool",
            "Dimes",
            "gazoz",
            "içecek",
            "icecek",
            "meyve suyu",
            "nektar",
            "soda",
            "kola",
            "aromalı",
            "aromali",
            "şurup",
            "surup",
            "bisküvi",
            "biskuvi",
            "bisküit",
            "kek",
            "Sprite",
            "Schweppes",
            "Pepsi",
            "Lipton",
            "Cappy",
            "Tamek",
            "Pınar",
            "Enerji",
            "Energy",
            "buzlu",
            " ml",
            "şişe",
            "kutu",
            "teneke",
            "Cepte Şok",
            "Cepte Sok");
    }

    private static bool ContainsAnyText(string value, params string[] needles)
    {
        var normalized = FixMojibake(value).ToLowerInvariant();

        return needles.Any(needle => normalized.Contains(FixMojibake(needle).ToLowerInvariant()));
    }

    private static string NormalizeMarketName(string value)
    {
        return FixMojibake(value).Trim();
    }

    private static string FixMojibake(string value)
    {
        if (!value.Contains('?') && !value.Contains('?') && !value.Contains('?'))
        {
            return value;
        }

        try
        {
            return Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-1").GetBytes(value));
        }
        catch (ArgumentException)
        {
            return value;
        }
    }
}

internal sealed record MarketSeed(
    string Name,
    string? BaseUrl,
    string? SearchUrlTemplate,
    bool IsActive);



