using System.Diagnostics;
using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext    _dbContext;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
    {
        _logger    = logger;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var now      = DateTime.UtcNow;
        var cutoff60 = now.AddDays(-60);
        var cutoff14 = now.AddDays(-14);
        var cutoff7  = now.AddDays(-7);

        var products = await _dbContext.Products
            .Include(p => p.Varieties.Where(v => v.IsActive))
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var allRecords = await _dbContext.PriceRecords
            .Include(r => r.Market)
            .Where(r => r.IsLive && r.CheckedAt >= cutoff60)
            .OrderByDescending(r => r.CheckedAt)
            .ToListAsync();

        // Aktif ürünlerin çeşitlerine bağlı tüm ProductMarketLink'ler
        var allVarietyIds = products
            .SelectMany(p => p.Varieties.Where(v => v.IsActive).Select(v => v.Id))
            .ToList();

        var allLinks = allVarietyIds.Count > 0
            ? await _dbContext.ProductMarketLinks
                .Include(l => l.Market)
                .Where(l => l.IsActive && allVarietyIds.Contains(l.ProductVarietyId))
                .ToListAsync()
            : new List<ProductMarketLink>();

        // Tüm bağlı marketler (KPI strip için global liste)
        var matrixMarkets = allLinks
            .Select(l => l.Market)
            .DistinctBy(m => m.Id)
            .Select(m => new MarketColumn { Id = m.Id, Name = m.Name, BaseUrl = m.BaseUrl ?? "" })
            .OrderBy(m => m.IsSpecialty ? 0 : 1)
            .ThenBy(m => m.Name)
            .ToList();

        var productImageMap = allRecords
            .Where(r => !string.IsNullOrWhiteSpace(r.ImageUrl))
            .GroupBy(r => r.ProductId)
            .ToDictionary(g => g.Key,
                g => g.OrderByDescending(r => r.CheckedAt).First().ImageUrl!);

        var categoryOrder = new Dictionary<string,int>
        {
            ["Narenciye"]=0,["Tropikal"]=1,["Meyve"]=2,
            ["Sebze"]=3,["İşlenmiş"]=4,["Diğer"]=5
        };

        var productRows = products
            .OrderBy(p => categoryOrder.GetValueOrDefault(p.Category, 99))
            .ThenBy(p => p.Name)
            .Select(product =>
            {
                var prodRecs = allRecords.Where(r => r.ProductId == product.Id).ToList();

                // Bu ürünün herhangi bir çeşidine bağlı linkler
                var productLinks = allLinks
                    .Where(l => product.Varieties.Any(v => v.Id == l.ProductVarietyId))
                    .ToList();

                // Ürün bazlı matris sütunları (fiyat olsun olmasın)
                var productMarkets = productLinks
                    .Select(l => l.Market)
                    .DistinctBy(m => m.Id)
                    .Select(m => new MarketColumn { Id = m.Id, Name = m.Name, BaseUrl = m.BaseUrl ?? "" })
                    .OrderBy(m => m.IsSpecialty ? 0 : 1)
                    .ThenBy(m => m.Name)
                    .ToList();

                var hasVarietyTagged = prodRecs.Any(r => r.ProductVarietyId.HasValue);
                var activeVarieties  = product.Varieties.Where(v => v.IsActive).OrderBy(v => v.Name).ToList();

                List<ProductVariety> varieties;
                if (activeVarieties.Count == 0 || !hasVarietyTagged)
                {
                    varieties = [new ProductVariety
                    {
                        Id        = 0,
                        Name      = "Genel",
                        ProductId = product.Id,
                        Notes     = !hasVarietyTagged && activeVarieties.Count > 0
                            ? "Çeşit bazlı fiyat verisi henüz yok. Güncelle ile çeşit bazlı veri çekilecek."
                            : null,
                    }];
                }
                else
                {
                    varieties = activeVarieties;
                }

                var varietyRows = varieties.Select(variety =>
                {
                    var allVarRecs = variety.Id == 0
                        ? prodRecs
                        : prodRecs.Where(r => r.ProductVarietyId == variety.Id).ToList();

                    // Bu çeşide ait linkler (Genel satırı için tüm ürün linkleri)
                    var varietyLinks = variety.Id == 0
                        ? productLinks
                        : productLinks.Where(l => l.ProductVarietyId == variety.Id).ToList();

                    var linkedMarketIds  = varietyLinks.Select(l => l.MarketId).ToHashSet();
                    var marketDirectUrls = varietyLinks
                        .GroupBy(l => l.MarketId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.FirstOrDefault(l => !string.IsNullOrEmpty(l.DirectUrl))?.DirectUrl ?? "");

                    // Her market için en güncel fiyat (ürün bazlı sütunlardan)
                    var marketPrices = productMarkets.ToDictionary(
                        m => m.Id,
                        m => (PriceRecord?)allVarRecs
                            .Where(r => r.MarketId == m.Id)
                            .MaxBy(r => r.CheckedAt)
                    );

                    // Sparkline (14 günlük)
                    var spark = allVarRecs
                        .Where(r => r.CheckedAt >= cutoff14)
                        .GroupBy(r => r.CheckedAt.ToLocalTime().Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new SparkPoint(g.Key, g.Min(r => r.PricePerKg)))
                        .ToList();

                    // Trend: son 14 gün vs önceki dönem
                    decimal? trend = null;
                    var recentBest = allVarRecs.Where(r => r.CheckedAt >= cutoff14).MinBy(r => r.PricePerKg);
                    var olderBest  = allVarRecs.Where(r => r.CheckedAt < cutoff14).MinBy(r => r.PricePerKg);
                    if (recentBest != null && olderBest != null && olderBest.PricePerKg > 0)
                        trend = Math.Round((recentBest.PricePerKg - olderBest.PricePerKg) / olderBest.PricePerKg * 100m, 1);

                    // Mevsim durumu
                    var season = DetectSeason(variety.HarvestPeriod);

                    return new VarietyPriceRow
                    {
                        Variety          = variety,
                        MarketPrices     = marketPrices,
                        LinkedMarketIds  = linkedMarketIds,
                        MarketDirectUrls = marketDirectUrls,
                        SparklinePoints  = spark,
                        TrendPct         = trend,
                        Season           = season,
                    };
                }).ToList();

                var productSpark = prodRecs
                    .Where(r => r.CheckedAt >= cutoff14)
                    .GroupBy(r => r.CheckedAt.ToLocalTime().Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new SparkPoint(g.Key, g.Min(r => r.PricePerKg)))
                    .ToList();

                var bestImg = prodRecs
                    .Where(r => !string.IsNullOrWhiteSpace(r.ImageUrl))
                    .OrderByDescending(r => r.CheckedAt)
                    .FirstOrDefault()?.ImageUrl
                    ?? productImageMap.GetValueOrDefault(product.Id)
                    ?? product.ImageUrl;

                return new ProductDashboardRow
                {
                    Product          = product,
                    ProductMarkets   = productMarkets,
                    Varieties        = varietyRows,
                    ProductSparkline = productSpark,
                    BestImageUrl     = bestImg,
                };
            }).ToList();

        var model = new DashboardViewModel
        {
            ProductRows   = productRows,
            MatrixMarkets = matrixMarkets,
            TotalProducts = products.Count,
            TotalMarkets  = await _dbContext.Markets.CountAsync(m => m.IsActive),
            TotalRecords  = await _dbContext.PriceRecords.CountAsync(),
        };

        return View(model);
    }

    // Hasat dönemi metninden mevsim durumu hesapla
    private static SeasonStatus DetectSeason(string? harvestPeriod)
    {
        if (string.IsNullOrWhiteSpace(harvestPeriod)) return SeasonStatus.Unknown;

        var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["ocak"]=1,["şubat"]=2,["mart"]=3,["nisan"]=4,["mayıs"]=5,["haziran"]=6,
            ["temmuz"]=7,["ağustos"]=8,["eylül"]=9,["ekim"]=10,["kasım"]=11,["aralık"]=12,
        };

        var cur  = DateTime.Now.Month;
        var text = harvestPeriod.ToLowerInvariant();
        var months = monthMap
            .Where(kv => text.Contains(kv.Key))
            .Select(kv => kv.Value)
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        if (months.Count == 0) return SeasonStatus.Unknown;

        // Tüm aylık aralığı genişlet
        int start = months.First(), end = months.Last();

        bool inRange;
        if (start <= end)
            inRange = cur >= start && cur <= end;
        else // aralık yıl sonunu aşıyor (örn. Kasım-Mart)
            inRange = cur >= start || cur <= end;

        if (inRange) return SeasonStatus.InSeason;

        // "Yaklaşıyor" → bir ay sonra başlayacak
        int next = cur % 12 + 1;
        bool approaching = start <= end
            ? next >= start && next <= end
            : next >= start || next <= end;

        return approaching ? SeasonStatus.Approaching : SeasonStatus.OffSeason;
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
