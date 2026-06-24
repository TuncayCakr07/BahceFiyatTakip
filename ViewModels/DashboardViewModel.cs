using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.ViewModels;

public class DashboardViewModel
{
    public int ProductCount { get; set; }
    public int MarketCount { get; set; }
    public int PriceRecordCount { get; set; }
    public IReadOnlyList<ProductPriceSummary> ProductSummaries { get; set; } = [];
}

public class ProductPriceSummary
{
    public Product Product { get; set; } = null!;
    public IReadOnlyList<PriceRecord> Prices { get; set; } = [];
    public DateTime? LastChecked { get; set; }
    public PriceRecord? Cheapest => Prices.Count > 0 ? Prices[0] : null;
}
