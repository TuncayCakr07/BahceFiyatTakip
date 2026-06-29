using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices.PlatformDetection;

public interface IMarketPlatformDetector
{
    Task<PlatformDetectionResult> DetectAsync(Market market, CancellationToken cancellationToken = default);
}
