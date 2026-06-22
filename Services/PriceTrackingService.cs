using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Services;

public interface IPriceTrackingService
{
    Task<IReadOnlyList<PriceRecord>> CheckAndSavePricesAsync(int productId, CancellationToken cancellationToken = default);
}

public class PriceTrackingService(
    ApplicationDbContext dbContext,
    IMarketPriceProvider marketPriceProvider) : IPriceTrackingService
{
    public async Task<IReadOnlyList<PriceRecord>> CheckAndSavePricesAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(item => item.Varieties)
                .ThenInclude(variety => variety.SearchAliases)
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Urun bulunamadi veya aktif degil.");

        var markets = await dbContext.Markets
            .Where(market => market.IsActive)
            .OrderBy(market => market.Name)
            .ToListAsync(cancellationToken);

        var priceResults = await marketPriceProvider.GetPricesAsync(product, markets, cancellationToken);
        var checkedAt = DateTime.UtcNow;

        var records = priceResults.Select(result => new PriceRecord
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
            IsLive = result.IsLive,
            ConfidenceScore = result.ConfidenceScore
        }).ToList();

        dbContext.PriceRecords.AddRange(records);
        await dbContext.SaveChangesAsync(cancellationToken);

        return records;
    }
}
