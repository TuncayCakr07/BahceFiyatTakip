namespace BahceFiyatTakip.Services.MarketPrices.Routing;

public record AdapterRouteResult(
    int MarketId,
    string MarketName,
    BahceFiyatTakip.Services.MarketPrices.PlatformDetection.MarketPlatform Platform,
    string AdapterName,
    bool CanHandle,
    string Reason);
