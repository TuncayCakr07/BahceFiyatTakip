using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Services.MarketPrices.PlatformDetection;

public class PlatformDetectionReportService(
    ApplicationDbContext dbContext,
    IMarketPlatformDetector detector,
    ILogger<PlatformDetectionReportService> logger)
{
    public async Task<IReadOnlyList<PlatformDetectionResult>> GenerateReportAsync(CancellationToken ct = default)
    {
        var markets = await dbContext.Markets
            .Where(m => m.IsActive)
            .ToListAsync(ct);

        var results = new List<PlatformDetectionResult>();
        foreach (var market in markets)
        {
            try
            {
                var result = await detector.DetectAsync(market, ct);
                results.Add(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to detect platform for {Market}", market.Name);
            }
        }

        return results;
    }
}
