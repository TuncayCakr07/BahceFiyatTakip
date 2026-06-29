using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices.PlatformDetection;

namespace BahceFiyatTakip.Services.MarketPrices.Routing;

public class MarketAdapterRouter(
    IMarketPlatformDetector platformDetector) : IMarketAdapterRouter
{
    public async Task<AdapterRouteResult> ResolveAsync(Market market, CancellationToken cancellationToken = default)
    {
        var detection = await platformDetector.DetectAsync(market, cancellationToken);
        
        var (adapterName, canHandle) = detection.Platform switch
        {
            MarketPlatform.Macrocenter  => ("MacrocenterPriceAdapter", true),
            MarketPlatform.Migros       => ("MigrosPriceAdapter", true),
            MarketPlatform.CarrefourSA  => ("CarrefourSAPriceAdapter", true),
            MarketPlatform.GenericJsonLd => ("GenericJsonLdPriceAdapter", true),
            MarketPlatform.GenericNextJs => ("GenericNextJsPriceAdapter", true),
            MarketPlatform.Ticimax      => ("GenericTicimaxPriceAdapter", true),
            
            // Placeholders for future generic adapters
            MarketPlatform.SapCommerce  => ("GenericSapCommercePriceAdapter", false),
            MarketPlatform.WooCommerce  => ("GenericWooCommercePriceAdapter", false),
            MarketPlatform.IdeaSoft     => ("GenericIdeaSoftPriceAdapter", false),
            MarketPlatform.NopCommerce  => ("GenericNopCommercePriceAdapter", false),
            MarketPlatform.GraphQL      => ("GenericGraphQlPriceAdapter", false),
            MarketPlatform.RestApi      => ("GenericRestApiPriceAdapter", false),
            
            _ => ("None", false)
        };

        string reason = canHandle 
            ? $"Platform {detection.Platform} mapped to existing adapter {adapterName}" 
            : $"Platform {detection.Platform} detected, but no implementation exists for {adapterName}";

        return new AdapterRouteResult(
            market.Id, 
            market.Name, 
            detection.Platform, 
            adapterName, 
            canHandle, 
            reason);
    }
}
