using BahceFiyatTakip.Data;
using BahceFiyatTakip.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Services;

public class MarketHealthService(ApplicationDbContext dbContext)
{
    public async Task<List<MarketHealthInfo>> GetHealthAsync()
    {
        var cutoff = DateTime.Now.AddDays(-7);

        var markets = await dbContext.Markets
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();

        var activeMarketIds = await dbContext.ProductMarketLinks
            .Where(l => l.IsActive)
            .Select(l => l.MarketId)
            .Distinct()
            .ToListAsync();

        var activeMarketSet = activeMarketIds.ToHashSet();

        // Sistemin son 7 günde aktif olduğu günler (herhangi bir market başarılı olduysa)
        var activeDays = await dbContext.PriceRecords
            .Where(r => r.IsLive && r.CheckedAt >= cutoff)
            .Select(r => r.CheckedAt.Date)
            .Distinct()
            .ToListAsync();

        // Market bazlı son başarı + kayıt sayısı
        var marketStats = await dbContext.PriceRecords
            .Where(r => r.IsLive && r.CheckedAt >= cutoff)
            .GroupBy(r => r.MarketId)
            .Select(g => new
            {
                MarketId      = g.Key,
                LastSuccessAt = g.Max(r => r.CheckedAt),
                RecordCount   = g.Count(),
            })
            .ToListAsync();

        // Market bazlı başarılı gün listesi
        var marketSuccessDayRows = await dbContext.PriceRecords
            .Where(r => r.IsLive && r.CheckedAt >= cutoff)
            .Select(r => new { r.MarketId, Day = r.CheckedAt.Date })
            .Distinct()
            .ToListAsync();

        var statsByMarket = marketStats.ToDictionary(x => x.MarketId);
        var successDaysByMarket = marketSuccessDayRows
            .GroupBy(x => x.MarketId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Day).ToHashSet());

        var result = markets
            .Where(m => activeMarketSet.Contains(m.Id))
            .Select(m =>
            {
                var stats       = statsByMarket.GetValueOrDefault(m.Id);
                var successDays = successDaysByMarket.GetValueOrDefault(m.Id) ?? [];

                if (activeDays.Count == 0 || stats == null)
                {
                    return new MarketHealthInfo
                    {
                        MarketId   = m.Id,
                        MarketName = m.Name,
                        Status     = HealthStatus.Unknown,
                    };
                }

                var successCount  = successDays.Count;
                var expectedCount = activeDays.Count;
                var failureCount  = Math.Max(0, expectedCount - successCount);
                var successRate   = Math.Round(successCount / (double)expectedCount * 100.0, 1);

                // En son aktif günlerden bu marketin kaçırdığı son gün
                var lastFailureDate = activeDays
                    .Where(d => !successDays.Contains(d))
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                var status = successRate >= 90.0 ? HealthStatus.Healthy
                           : successRate >= 60.0 ? HealthStatus.Warning
                           : HealthStatus.Failed;

                return new MarketHealthInfo
                {
                    MarketId          = m.Id,
                    MarketName        = m.Name,
                    LastSuccessAt     = stats.LastSuccessAt,
                    LastFailureAt     = lastFailureDate == default ? null : (DateTime?)lastFailureDate,
                    SuccessCount      = successCount,
                    FailureCount      = failureCount,
                    SuccessRate       = successRate,
                    AverageResponseMs = -1,
                    Status            = status,
                };
            })
            .OrderBy(h => h.Status switch
            {
                HealthStatus.Failed  => 0,
                HealthStatus.Warning => 1,
                HealthStatus.Unknown => 2,
                _                    => 3,
            })
            .ThenBy(h => h.MarketName)
            .ToList();

        return result;
    }
}
