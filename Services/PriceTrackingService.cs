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

        var checkedAt = DateTime.Now;

        var records = reliableLiveResults.Select(result => new PriceRecord
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
            ConfidenceScore = result.ConfidenceScore
        }).ToList();

        dbContext.PriceRecords.AddRange(records);
        await dbContext.SaveChangesAsync(cancellationToken);

        return records;
    }

    private async Task EnsureMarketCatalogAsync(CancellationToken cancellationToken)
    {
        var catalog = MarketCatalogSeed.All;

        var existingMarkets = await dbContext.Markets.ToListAsync(cancellationToken);

        foreach (var item in catalog)
        {
            var existing = existingMarkets.FirstOrDefault(market =>
                string.Equals(market.Name, item.Name, StringComparison.OrdinalIgnoreCase));

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

            existing.BaseUrl = item.BaseUrl;
            existing.SearchUrlTemplate = item.SearchUrlTemplate;
            existing.IsActive = item.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}



