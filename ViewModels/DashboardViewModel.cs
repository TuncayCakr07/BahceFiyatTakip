using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.ViewModels;

public class DashboardViewModel
{
    public int ProductCount { get; set; }

    public int MarketCount { get; set; }

    public int PriceRecordCount { get; set; }

    public IReadOnlyList<PriceRecord> LatestPrices { get; set; } = [];
}
