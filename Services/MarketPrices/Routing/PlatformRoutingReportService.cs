using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Services.MarketPrices.Routing;

public class PlatformRoutingReportService(
    ApplicationDbContext dbContext,
    IMarketAdapterRouter router,
    ILogger<PlatformRoutingReportService> logger)
{
    public async Task<IReadOnlyList<AdapterRouteResult>> GenerateRoutingReportAsync(CancellationToken ct = default)
    {
        var markets = await dbContext.Markets
            .Where(m => m.IsActive)
            .ToListAsync(ct);

        var results = new List<AdapterRouteResult>();
        foreach (var market in markets)
        {
            try
            {
                var result = await router.ResolveAsync(market, ct);
                results.Add(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Routing resolution failed for {Market}", market.Name);
            }
        }

        return results;
    }
}
