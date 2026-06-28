using System.Diagnostics;
using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices;
using BahceFiyatTakip.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController>  _logger;
    private readonly ApplicationDbContext     _dbContext;
    private readonly LiveMarketPriceProvider  _liveProvider;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext, LiveMarketPriceProvider liveProvider)
    {
        _logger       = logger;
        _dbContext    = dbContext;
        _liveProvider = liveProvider;
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

    // Son 30 günlük fiyat geçmişi — Chart.js uyumlu JSON
    [HttpGet]
    public async Task<IActionResult> PriceHistory(int productId)
    {
        var cutoff = DateTime.Now.AddDays(-30).Date;

        var records = await _dbContext.PriceRecords
            .Include(r => r.Market)
            .Where(r => r.ProductId == productId && r.CheckedAt >= cutoff)
            .OrderBy(r => r.CheckedAt)
            .ToListAsync();

        var productName = await _dbContext.Products
            .Where(p => p.Id == productId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync() ?? "";

        if (records.Count == 0)
            return Json(new PriceHistoryResponse(productId, productName, [], []));

        // Distinct günler (yerel tarih, artan sıra)
        var dates = records
            .Select(r => r.CheckedAt.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var labels = dates.Select(d => d.ToString("yyyy-MM-dd")).ToList();

        // Market başına, her güne hizalı fiyat dizisi (o gün yoksa null)
        var datasets = records
            .GroupBy(r => new { r.MarketId, MarketName = r.Market.Name })
            .Select(g =>
            {
                var byDate = g
                    .GroupBy(r => r.CheckedAt.Date)
                    .ToDictionary(x => x.Key, x => x.Min(r => r.PricePerKg));

                var data = dates
                    .Select(d => byDate.TryGetValue(d, out var p) ? (decimal?)p : null)
                    .ToList();

                return new PriceHistorySeries(g.Key.MarketId, g.Key.MarketName, data);
            })
            .OrderBy(s => s.MarketName)
            .ToList();

        return Json(new PriceHistoryResponse(productId, productName, labels, datasets));
    }

    // Eksik URL Yönetim Ekranı
    [HttpGet]
    public IActionResult UrlManagement() => View();

    // DirectUrl test — AJAX
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestDirectUrl([FromBody] TestDirectUrlRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
            return Ok(new { success = false, error = "URL boş" });
        var (found, title, price, error) = await _liveProvider.TestDirectUrlAsync(req.Url);
        return Ok(new { success = found, title, price, error });
    }

    // DirectUrl kaydet — AJAX
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDirectUrl([FromBody] SaveDirectUrlRequest req)
    {
        var link = await _dbContext.ProductMarketLinks
            .FirstOrDefaultAsync(l => l.ProductVarietyId == req.VarietyId && l.MarketId == req.MarketId);
        if (link == null) return NotFound(new { error = "Kayıt bulunamadı" });
        link.DirectUrl = req.DirectUrl?.Trim() ?? "";
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // Eksik URL Raporu — ProductVariety × Market matrisi
    [HttpGet]
    public async Task<IActionResult> UrlReport()
    {
        var todayUtc = DateTime.UtcNow.Date;
        var cutoff   = todayUtc.AddDays(-60);

        var links = await _dbContext.ProductMarketLinks
            .Include(l => l.Market)
            .Include(l => l.ProductVariety)
                .ThenInclude(v => v.Product)
            .Where(l => l.IsActive
                     && l.ProductVariety != null
                     && l.ProductVariety.IsActive
                     && l.ProductVariety.Product != null
                     && l.ProductVariety.Product.IsActive)
            .OrderBy(l => l.ProductVariety.Product.Category)
            .ThenBy(l => l.ProductVariety.Product.Name)
            .ThenBy(l => l.ProductVariety.Name)
            .ThenBy(l => l.Market.Name)
            .ToListAsync();

        // Son 60 günlük kayıtlar — (productId, marketId) bazında en son fiyat
        var recentRecords = await _dbContext.PriceRecords
            .Where(r => r.IsLive && r.CheckedAt >= cutoff)
            .Select(r => new { r.ProductId, r.MarketId, r.CheckedAt, r.PricePerKg })
            .OrderByDescending(r => r.CheckedAt)
            .ToListAsync();

        var todaySet = recentRecords
            .Where(r => r.CheckedAt >= todayUtc)
            .Select(r => (r.ProductId, r.MarketId))
            .ToHashSet();

        var lastByKey = recentRecords
            .GroupBy(r => (r.ProductId, r.MarketId))
            .ToDictionary(g => g.Key, g => g.First());

        var rows = links.Select(l =>
        {
            var pid = l.ProductVariety.ProductId;
            var key = (pid, l.MarketId);

            string status;
            if (!l.Market.IsActive)
                status = "MarketInactive";
            else if (string.IsNullOrWhiteSpace(l.DirectUrl))
                status = "NoUrl";
            else if (todaySet.Contains(key))
                status = "OkToday";
            else
                status = "UrlExistsNoPrice";

            lastByKey.TryGetValue(key, out var last);

            return new UrlReportEntry(
                pid, l.ProductVariety.Product.Name, l.ProductVariety.Product.Category,
                l.ProductVarietyId, l.ProductVariety.Name,
                l.MarketId, l.Market.Name, l.Market.IsActive,
                string.IsNullOrWhiteSpace(l.DirectUrl) ? null : l.DirectUrl,
                status,
                last?.CheckedAt, last?.PricePerKg);
        }).ToList();

        return Json(new UrlReportResponse(
            DateTime.UtcNow,
            rows.Count(r => r.Status == "OkToday"),
            rows.Count(r => r.Status == "UrlExistsNoPrice"),
            rows.Count(r => r.Status == "NoUrl"),
            rows.Count(r => r.Status == "MarketInactive"),
            rows));
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
