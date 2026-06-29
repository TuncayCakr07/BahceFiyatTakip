using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Services;

public interface IPriceTrackingService
{
    Task<IReadOnlyList<PriceRecord>> CheckAndSavePricesAsync(
        int productId,
        CancellationToken cancellationToken = default);
}

public class PriceTrackingService(
    ApplicationDbContext dbContext,
    IMarketPriceProvider marketPriceProvider) : IPriceTrackingService
{
public async Task<IReadOnlyList<PriceRecord>> CheckAndSavePricesAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        await EnsureMarketCatalogAsync(cancellationToken);

        var product = await dbContext.Products
            .Include(item => item.Varieties)
                .ThenInclude(variety => variety.SearchAliases)
            .Include(item => item.Varieties)
                .ThenInclude(variety => variety.DirectLinks)
            .FirstOrDefaultAsync(
                item => item.Id == productId && item.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Urun bulunamadi veya aktif degil.");

        var markets = await dbContext.Markets
            .OrderBy(market => market.Name)
            .ToListAsync(cancellationToken);

        var priceResults = await marketPriceProvider.GetPricesAsync(
            product,
            markets,
            cancellationToken);

        var reliableLiveResults = priceResults
            .Where(result =>
                result.IsLive &&
                !string.Equals(result.ProviderName, "Mock", StringComparison.OrdinalIgnoreCase) &&
                !result.ProviderName.Contains("Mock", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (reliableLiveResults.Count == 0)
        {
            return [];
        }

        // Bugün aynı (ProductVarietyId, MarketId) için kayıt varsa skip
        var todayLocal = DateTime.Today;
        var existingTodayKeys = await dbContext.PriceRecords
            .Where(r => r.ProductId == productId && r.CheckedAt >= todayLocal)
            .Select(r => new { r.ProductVarietyId, r.MarketId })
            .ToListAsync(cancellationToken);

        var todayKeySet = existingTodayKeys
            .Select(k => (k.ProductVarietyId, k.MarketId))
            .ToHashSet();

        var newResults = reliableLiveResults
            .Where(r => !todayKeySet.Contains((r.ProductVarietyId, r.MarketId)))
            .ToList();

        if (newResults.Count == 0)
        {
            return [];
        }

        var checkedAt = DateTime.Now;

        var records = newResults.Select(result => new PriceRecord
        {
            ProductId = product.Id,
            ProductVarietyId = result.ProductVarietyId,
            MarketId = result.MarketId,
            PricePerKg = result.PricePerKg,
            CheckedAt = checkedAt,
            SourceUrl = result.SourceUrl,
            ImageUrl = result.ImageUrl,
            MatchedTitle = result.MatchedTitle,
            SourceProvider = result.ProviderName,
            IsLive = true,
            InStock = result.InStock,
            ConfidenceScore = result.ConfidenceScore
        }).ToList();

        dbContext.PriceRecords.AddRange(records);

        // İlk başarılı scrape'de Product.ImageUrl'u doldur (kalıcı fallback)
        if (string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            var firstImg = records.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.ImageUrl))?.ImageUrl;
            if (firstImg is not null)
            {
                product.ImageUrl = firstImg;
                dbContext.Products.Update(product);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return records;
    }

    private async Task EnsureMarketCatalogAsync(CancellationToken cancellationToken)
    {
        var catalog = MarketCatalogSeed.All;

        var existingMarkets = await dbContext.Markets.ToListAsync(cancellationToken);

        foreach (var item in catalog)
        {
            // Önce tam eşleşme (büyük/küçük harf farkı yok)
            var existing = existingMarkets.FirstOrDefault(m =>
                string.Equals(m.Name, item.Name, StringComparison.OrdinalIgnoreCase));

            // Boşluk normalleştirmesiyle eşleşme: "Hediyelik Bahçem" == "Hediyelikbahçem" duplikasyonunu önler
            if (existing is null)
            {
                var norm = item.Name.Replace(" ", "").ToLowerInvariant();
                existing = existingMarkets.FirstOrDefault(m =>
                    m.Name.Replace(" ", "").ToLowerInvariant() == norm);
            }

            if (existing is null)
            {
                dbContext.Markets.Add(new Market
                {
                    Name              = item.Name,
                    BaseUrl           = item.BaseUrl,
                    SearchUrlTemplate = item.SearchUrlTemplate,
                    IsActive          = item.IsActive
                });
                continue;
            }

            // BaseUrl / SearchUrlTemplate: sadece mevcut kayıt boşsa doldur
            if (!string.IsNullOrEmpty(item.BaseUrl) && string.IsNullOrEmpty(existing.BaseUrl))
                existing.BaseUrl = item.BaseUrl;
            if (!string.IsNullOrEmpty(item.SearchUrlTemplate) && string.IsNullOrEmpty(existing.SearchUrlTemplate))
                existing.SearchUrlTemplate = item.SearchUrlTemplate;
            // Aktif bir marketi pasife çekme; catalog aktifleştirebilir ama devre dışı bırakamaz
            existing.IsActive = existing.IsActive || item.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}



