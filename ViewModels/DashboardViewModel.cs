using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.ViewModels;

public class DashboardViewModel
{
    public List<ProductDashboardRow> ProductRows   { get; set; } = [];
    public List<MarketColumn>        MatrixMarkets { get; set; } = [];
    public int TotalProducts  { get; set; }
    public int TotalMarkets   { get; set; }
    public int TotalRecords   { get; set; }

    public int VarietiesWithPrice => ProductRows.Sum(r => r.VarietiesWithPrice);
    public int DropCount          => ProductRows.Sum(r => r.Varieties.Count(v => v.HasPriceDrop));
    public int RiseCount          => ProductRows.Sum(r => r.Varieties.Count(v => v.HasPriceRise));
    public int TotalPrices        => ProductRows.Sum(r => r.TotalPriceCount);
    public DateTime? LastUpdate   => ProductRows
        .Select(r => r.LastChecked).Where(d => d.HasValue).Select(d => d!.Value)
        .DefaultIfEmpty().Max() is DateTime dt && dt != default ? dt : null;

    public List<MarketColumn> SpecialtyMarkets  => MatrixMarkets.Where(m => m.IsSpecialty).ToList();
    public List<MarketColumn> SuperMarkets      => MatrixMarkets.Where(m => !m.IsSpecialty).ToList();
}

public class ProductDashboardRow
{
    public Product               Product          { get; set; } = null!;
    public List<MarketColumn>    ProductMarkets   { get; set; } = [];
    public List<VarietyPriceRow> Varieties        { get; set; } = [];
    public string?               BestImageUrl     { get; set; }
    public List<SparkPoint>      ProductSparkline { get; set; } = [];

    public bool  HasAnyPrice        => Varieties.Any(v => v.HasAnyPrice);
    public int   VarietiesWithPrice => Varieties.Count(v => v.HasAnyPrice);
    public int   TotalPriceCount    => Varieties.Sum(v => v.MarketPrices.Values.Count(p => p != null));

    public PriceRecord? OverallCheapest => Varieties
        .Where(v => v.Cheapest != null).Select(v => v.Cheapest!).MinBy(p => p.PricePerKg);

    public decimal? BestTrendPct => Varieties.Where(v => v.TrendPct.HasValue)
        .Select(v => v.TrendPct!.Value).DefaultIfEmpty()
        .Min() is decimal t && Varieties.Any(v => v.TrendPct.HasValue) ? t : null;

    public DateTime? LastChecked => Varieties
        .SelectMany(v => v.MarketPrices.Values).Where(p => p != null)
        .Select(p => p!.CheckedAt).DefaultIfEmpty()
        .Max() is DateTime dt && dt != default ? dt : null;
}

public class VarietyPriceRow
{
    public ProductVariety                Variety          { get; set; } = null!;
    public Dictionary<int, PriceRecord?> MarketPrices    { get; set; } = [];
    public HashSet<int>                  LinkedMarketIds  { get; set; } = [];
    public Dictionary<int, string>       MarketDirectUrls { get; set; } = [];
    public List<SparkPoint>              SparklinePoints  { get; set; } = [];
    public decimal?                      TrendPct         { get; set; }
    public SeasonStatus                  Season           { get; set; } = SeasonStatus.Unknown;

    public PriceRecord? Cheapest   => MarketPrices.Values.Where(p => p != null).MinBy(p => p!.PricePerKg);
    public bool HasAnyPrice        => MarketPrices.Values.Any(p => p != null);
    public bool HasPriceDrop       => TrendPct.HasValue && TrendPct < -2m;
    public bool HasPriceRise       => TrendPct.HasValue && TrendPct > 2m;

    public DateTime? LastChecked => MarketPrices.Values.Where(p => p != null)
        .Select(p => p!.CheckedAt).DefaultIfEmpty()
        .Max() is DateTime dt && dt != default ? dt : null;
}

public enum SeasonStatus { Unknown, InSeason, OffSeason, Approaching }

public record SparkPoint(DateTime Date, decimal Price);
public enum DataFreshness { Fresh, Aging, Stale }

// PriceHistory API — Chart.js uyumlu response tipleri
public record PriceHistorySeries(int MarketId, string MarketName, List<decimal?> Data);
public record PriceHistoryResponse(
    int ProductId,
    string ProductName,
    List<string> Labels,
    List<PriceHistorySeries> Datasets);

// URL Report — Eksik URL Raporu
// Status: "OkToday" | "UrlExistsNoPrice" | "NoUrl" | "MarketInactive" | "OutOfStock"
public record UrlReportEntry(
    int      ProductId,   string   ProductName, string Category,
    int      VarietyId,  string   VarietyName,
    int      MarketId,   string   MarketName,  bool MarketActive,
    string?  DirectUrl,  string   Status,      string? Unit,
    DateTime? LastPriceAt, decimal? LastPrice);

public record SaveDirectUrlRequest(int VarietyId, int MarketId, string? DirectUrl);
public record TestDirectUrlRequest(string Url);

public record UrlReportResponse(
    DateTime GeneratedAt,
    int OkTodayCount, int UrlNoPriceCount, int NoUrlCount, int InactiveCount,
    int OutOfStockCount,
    List<UrlReportEntry> Rows);

public class MarketColumn
{
    public int    Id      { get; set; }
    public string Name    { get; set; } = "";
    public string BaseUrl { get; set; } = "";

    private static readonly HashSet<string> SupermarketDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "migros.com.tr", "carrefoursa.com", "a101.com.tr",
        "sokmarket.com.tr", "filemarket.com.tr", "macrocenter.com.tr",
        "bim.com.tr", "hakmar.com.tr", "teknosa.com", "mediamarkt.com.tr"
    };

    public bool IsSpecialty => !SupermarketDomains.Any(d =>
        BaseUrl.Contains(d, StringComparison.OrdinalIgnoreCase));
}
