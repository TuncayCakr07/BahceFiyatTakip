using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

public class CompositeMarketPriceProvider(
    LiveMarketPriceProvider liveProvider,
    BraveWebSearchPriceProvider braveProvider) : IMarketPriceProvider
{
    private const int MinimumConfidenceScore = 60;

    public string ProviderName => "LiveThenBrave";

    public async Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(
        Product product,
        IReadOnlyList<Market> markets,
        CancellationToken cancellationToken = default)
    {
        var liveResults = await liveProvider.GetPricesAsync(product, markets, cancellationToken);
        var reliableLiveResults = FilterReliableLiveResults(liveResults)
            .Where(result => !result.MarketName.Contains("Web Arama", StringComparison.OrdinalIgnoreCase))
            .OrderBy(result => result.MarketName)
            .ToList();

        if (reliableLiveResults.Count > 0)
        {
            return reliableLiveResults;
        }

        var braveResults = await braveProvider.GetPricesAsync(product, markets, cancellationToken);

        return FilterReliableLiveResults(braveResults)
            .OrderBy(result => result.MarketName)
            .ToList();
    }

    private static IEnumerable<MarketPriceResult> FilterReliableLiveResults(IEnumerable<MarketPriceResult> results)
    {
        return results.Where(result =>
            result.IsLive &&
            result.ConfidenceScore >= MinimumConfidenceScore &&
            !string.Equals(result.ProviderName, "Mock", StringComparison.OrdinalIgnoreCase) &&
            !result.ProviderName.Contains("Mock", StringComparison.OrdinalIgnoreCase));
    }
}
