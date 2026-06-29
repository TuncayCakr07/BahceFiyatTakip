using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace BahceFiyatTakip.Services.MarketPrices.Routing;

public class MarketAdapterExecutor(
    IMarketAdapterRouter router,
    IServiceProvider serviceProvider,
    ILogger<MarketAdapterExecutor> logger) : IMarketAdapterExecutor
{
    public async Task<MarketPriceResult?> TryFetchDirectAsync(
        Market market, 
        string directUrl, 
        Product product, 
        ProductVariety? variety, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var route = await router.ResolveAsync(market, cancellationToken);
            if (!route.CanHandle)
            {
                logger.LogDebug("Router: {Market} for {Platform} cannot be handled by any active adapter.", market.Name, route.Platform);
                return null;
            }

            logger.LogInformation("Router: {Market} mapped to {Adapter}. Attempting direct fetch.", market.Name, route.AdapterName);

            // Dinamik resolver: IServiceProvider üzerinden ilgili adapter'ı çağır
            return route.AdapterName switch
            {
                "MacrocenterPriceAdapter" => await GetAdapter<MacrocenterPriceAdapter>().TryFetchAsync(
                    market, directUrl, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "MigrosPriceAdapter" => await GetAdapter<MigrosPriceAdapter>().TryFetchDirectAsync(
                    market, directUrl, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "CarrefourSAPriceAdapter" => await GetAdapter<CarrefourSAPriceAdapter>().TryFetchDirectAsync(
                    market, directUrl, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "GenericJsonLdPriceAdapter" => await GetAdapter<GenericJsonLdPriceAdapter>().TryFetchDirectAsync(
                    market, directUrl, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "GenericNextJsPriceAdapter" => await GetAdapter<GenericNextJsPriceAdapter>().TryFetchDirectAsync(
                    market, directUrl, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "GenericTicimaxPriceAdapter" => await GetAdapter<GenericTicimaxPriceAdapter>().TryFetchDirectAsync(
                    market, directUrl, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "GenericSapCommercePriceAdapter" => await GetAdapter<GenericSapCommercePriceAdapter>().TryFetchDirectAsync(
                    market, directUrl, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                _ => null
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Executor error during direct fetch for {Market}", market.Name);
            return null;
        }
    }

    public async Task<MarketPriceResult?> TryFetchSearchAsync(
        Market market, 
        Product product, 
        ProductVariety? variety, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var route = await router.ResolveAsync(market, cancellationToken);
            if (!route.CanHandle)
            {
                logger.LogDebug("Router: {Market} for {Platform} cannot be handled by any active adapter.", market.Name, route.Platform);
                return null;
            }

            logger.LogInformation("Router: {Market} mapped to {Adapter}. Attempting search fetch.", market.Name, route.AdapterName);

            // Arama için variety null gelmiş olabilir, Product'ın ana query'sini kullan
            string query = variety?.SearchAliases.OrderBy(a => a.Priority).FirstOrDefault()?.Query 
                           ?? $"{variety?.Name} {product.Name}".ToLowerInvariant();

            return route.AdapterName switch
            {
                "MacrocenterPriceAdapter" => await GetAdapter<MacrocenterPriceAdapter>().TryFetchAsync(
                    market, query, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "MigrosPriceAdapter" => await GetAdapter<MigrosPriceAdapter>().TryFetchSearchAsync(
                    market, query, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "CarrefourSAPriceAdapter" => await GetAdapter<CarrefourSAPriceAdapter>().TryFetchSearchAsync(
                    market, query, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "GenericJsonLdPriceAdapter" => await GetAdapter<GenericJsonLdPriceAdapter>().TryFetchSearchAsync(
                    market, query, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "GenericNextJsPriceAdapter" => await GetAdapter<GenericNextJsPriceAdapter>().TryFetchSearchAsync(
                    market, query, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "GenericTicimaxPriceAdapter" => await GetAdapter<GenericTicimaxPriceAdapter>().TryFetchSearchAsync(
                    market, query, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                "GenericSapCommercePriceAdapter" => await GetAdapter<GenericSapCommercePriceAdapter>().TryFetchSearchAsync(
                    market, query, product.Name, variety?.Name ?? product.Name, variety?.Id, product.Unit, cancellationToken),
                _ => null
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Executor error during search fetch for {Market}", market.Name);
            return null;
        }
    }

    private T GetAdapter<T>() where T : class
    {
        return serviceProvider.GetRequiredService<T>();
    }
}
