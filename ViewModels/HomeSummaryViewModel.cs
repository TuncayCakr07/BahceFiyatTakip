namespace BahceFiyatTakip.ViewModels;

// Ana sayfa (Home/Index) — sade özet kartları
public class HomeSummaryViewModel
{
    public List<ProductSummaryCard> Products      { get; set; } = [];
    public int                      TotalProducts { get; set; }
    public int                      TotalMarkets  { get; set; }
    public DateTime?                LastUpdate    { get; set; }

    public int ProductsWithPrice => Products.Count(p => p.HasAnyPrice);
}

public class ProductSummaryCard
{
    public int       ProductId      { get; set; }
    public string    Name           { get; set; } = "";
    public string    Category       { get; set; } = "";
    public string    Unit           { get; set; } = "";
    public string?   ImageUrl       { get; set; }
    public decimal?  BestPrice      { get; set; }
    public string?   BestMarketName { get; set; }
    public decimal?  TrendPct       { get; set; }
    public DateTime? LastChecked    { get; set; }

    public bool HasAnyPrice => BestPrice.HasValue;
}
