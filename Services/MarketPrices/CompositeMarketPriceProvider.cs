using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

public class CompositeMarketPriceProvider(
    LiveMarketPriceProvider liveProvider,
    BraveWebSearchPriceProvider webSearchProvider,
    MockMarketPriceProvider mockProvider) : IMarketPriceProvider
{
    public string ProviderName => "LiveHtmlWithMockFallback";

    public async Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(Product product, IReadOnlyList<Market> markets, CancellationToken cancellationToken = default)
    {
        var liveResults = await liveProvider.GetPricesAsync(product, markets, cancellationToken);
        var webResults = await webSearchProvider.GetPricesAsync(product, markets, cancellationToken);
        var liveAndWebResults = liveResults.Concat(webResults).ToList();
        var completedMarketIds = liveAndWebResults.Select(result => result.MarketId).ToHashSet();
        var missingMarkets = markets.Where(market => !completedMarketIds.Contains(market.Id)).ToList();

        if (missingMarkets.Count == 0)
        {
            return liveAndWebResults;
        }

        var fallbackResults = await mockProvider.GetPricesAsync(product, missingMarkets, cancellationToken);

        return liveAndWebResults
            .Concat(fallbackResults)
            .OrderBy(result => result.MarketName)
            .ToList();
    }
}
