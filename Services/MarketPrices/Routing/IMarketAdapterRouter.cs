using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices.Routing;

public interface IMarketAdapterRouter
{
    Task<AdapterRouteResult> ResolveAsync(Market market, CancellationToken cancellationToken = default);
}
