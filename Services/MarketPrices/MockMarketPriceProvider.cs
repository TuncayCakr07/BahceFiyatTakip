using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

public class MockMarketPriceProvider(HttpClient httpClient) : IMarketPriceProvider
{
    public string ProviderName => "Mock";

    public Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(Product product, IReadOnlyList<Market> markets, CancellationToken cancellationToken = default)
    {
        _ = httpClient;

        var todaySeed = DateTime.UtcNow.Date.DayOfYear;
        var targets = BuildTargets(product);
        var results = markets
            .Where(market => market.IsActive)
            .SelectMany((market, marketIndex) => targets.Select(target =>
            {
                var basePrice = GetBasePrice(product.Name);
                var dailyMovement = ((todaySeed + product.Id + market.Id + (target.ProductVarietyId ?? 0)) % 11) - 5;
                var marketDifference = marketIndex * 1.35m;
                var varietyDifference = (target.ProductVarietyId ?? 0) % 7;
                var price = Math.Max(1m, basePrice + marketDifference + dailyMovement + varietyDifference);

                return new MarketPriceResult(
                    market.Id,
                    market.Name,
                    decimal.Round(price, 2),
                    market.BaseUrl,
                    ProviderName,
                    IsLive: false,
                    ProductVarietyId: target.ProductVarietyId,
                    MatchedTitle: target.Title,
                    ConfidenceScore: 25);
            }))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<MarketPriceResult>>(results);
    }

    private static IReadOnlyList<SearchTarget> BuildTargets(Product product)
    {
        var varieties = product.Varieties.Where(variety => variety.IsActive).ToList();
        if (varieties.Count == 0)
        {
            return [new SearchTarget(null, product.Name)];
        }

        return varieties
            .Select(variety => new SearchTarget(variety.Id, $"{variety.Name} {product.Name}"))
            .ToList();
    }

    private static decimal GetBasePrice(string productName)
    {
        if (productName.Contains("avokado", StringComparison.OrdinalIgnoreCase))
        {
            return 65m;
        }

        if (productName.Contains("lime", StringComparison.OrdinalIgnoreCase))
        {
            return 85m;
        }

        if (productName.Contains("portakal", StringComparison.OrdinalIgnoreCase))
        {
            return 28m;
        }

        if (productName.Contains("limon", StringComparison.OrdinalIgnoreCase))
        {
            return 32m;
        }

        return 24m;
    }

    private sealed record SearchTarget(int? ProductVarietyId, string Title);
}
