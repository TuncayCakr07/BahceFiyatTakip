using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices.Routing;

public interface IMarketAdapterExecutor
{
    Task<MarketPriceResult?> TryFetchDirectAsync(
        Market market, 
        string directUrl, 
        Product product, 
        ProductVariety? variety, 
        CancellationToken cancellationToken = default);

    Task<MarketPriceResult?> TryFetchSearchAsync(
        Market market, 
        Product product, 
        ProductVariety? variety, 
        CancellationToken cancellationToken = default);
}
