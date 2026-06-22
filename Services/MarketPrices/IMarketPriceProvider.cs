using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

public interface IMarketPriceProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(Product product, IReadOnlyList<Market> markets, CancellationToken cancellationToken = default);
}
