using System.Diagnostics;
using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services;
using BahceFiyatTakip.Services.MarketPrices;
using BahceFiyatTakip.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController>      _logger;
    private readonly ApplicationDbContext         _dbContext;
    private readonly LiveMarketPriceProvider      _liveProvider;
    private readonly MarketHealthService          _healthService;
    private readonly DirectUrlDiscoveryService    _discoveryService;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext,
        LiveMarketPriceProvider liveProvider, MarketHealthService healthService,
        DirectUrlDiscoveryService discoveryService)
    {
        _logger           = logger;
        _dbContext        = dbContext;
        _liveProvider     = liveProvider;
        _healthService    = healthService;
        _discoveryService = discoveryService;
    }

    // Ana sayfa — sade özet: kart başına en ucuz fiyat, trend ve detay linki.
    // Detaylı market × çeşit tablosu artık Products/Details sayfasında.
    public async Task<IActionResult> Index()
    {
        var now      = DateTime.UtcNow;
        var cutoff60 = now.AddDays(-60);
        var cutoff14 = now.AddDays(-14);

        var products = await _dbContext.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        var records = productIds.Count > 0
            ? await _dbContext.PriceRecords
                .Include(r => r.Market)
                .Where(r => productIds.Contains(r.ProductId) && r.IsLive && r.CheckedAt >= cutoff60)
                .ToListAsync()
            : new List<PriceRecord>();

        var categoryOrder = new Dictionary<string,int>
        {
            ["Narenciye"]=0,["Tropikal"]=1,["Meyve"]=2,
            ["Sebze"]=3,["İşlenmiş"]=4,["Diğer"]=5
        };

        var cards = products
            .OrderBy(p => categoryOrder.GetValueOrDefault(p.Category, 99))
            .ThenBy(p => p.Name)
            .Select(product =>
            {
                var recs        = records.Where(r => r.ProductId == product.Id).ToList();
                var inStockRecs = recs.Where(r => r.InStock != false).ToList();

                var best        = inStockRecs.MinBy(r => r.PricePerKg);
                var lastChecked = recs.Select(r => (DateTime?)r.CheckedAt).DefaultIfEmpty().Max();

                // Trend: son 14 gün vs önceki dönem
                decimal? trend = null;
                var recentBest = inStockRecs.Where(r => r.CheckedAt >= cutoff14).MinBy(r => r.PricePerKg);
                var olderBest  = inStockRecs.Where(r => r.CheckedAt < cutoff14).MinBy(r => r.PricePerKg);
                if (recentBest != null && olderBest != null && olderBest.PricePerKg > 0)
                    trend = Math.Round((recentBest.PricePerKg - olderBest.PricePerKg) / olderBest.PricePerKg * 100m, 1);

                var imageUrl = recs
                    .Where(r => !string.IsNullOrWhiteSpace(r.ImageUrl))
                    .OrderByDescending(r => r.CheckedAt)
                    .FirstOrDefault()?.ImageUrl
                    ?? product.ImageUrl;

                return new ProductSummaryCard
                {
                    ProductId      = product.Id,
                    Name           = product.Name,
                    Category       = product.Category,
                    Unit           = product.Unit,
                    ImageUrl       = imageUrl,
                    BestPrice      = best?.PricePerKg,
                    BestMarketName = best?.Market.Name,
                    TrendPct       = trend,
                    LastChecked    = lastChecked,
                };
            }).ToList();

        var model = new HomeSummaryViewModel
        {
            Products      = cards,
            TotalProducts = products.Count,
            TotalMarkets  = await _dbContext.Markets.CountAsync(m => m.IsActive),
            LastUpdate    = cards
                .Select(c => c.LastChecked).Where(d => d.HasValue).Select(d => d!.Value)
                .DefaultIfEmpty().Max() is DateTime dt && dt != default ? dt : null,
        };

        return View(model);
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
            .Select(r => new { r.ProductId, r.MarketId, r.CheckedAt, r.PricePerKg, r.InStock })
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

            lastByKey.TryGetValue(key, out var last);

            string status;
            if (!l.Market.IsActive)
                status = "MarketInactive";
            else if (string.IsNullOrWhiteSpace(l.DirectUrl))
                status = "NoUrl";
            else if (last?.InStock == false)
                status = "OutOfStock";
            else if (todaySet.Contains(key))
                status = "OkToday";
            else
                status = "UrlExistsNoPrice";

            return new UrlReportEntry(
                pid, l.ProductVariety.Product.Name, l.ProductVariety.Product.Category,
                l.ProductVarietyId, l.ProductVariety.Name,
                l.MarketId, l.Market.Name, l.Market.IsActive,
                string.IsNullOrWhiteSpace(l.DirectUrl) ? null : l.DirectUrl,
                status,
                l.ProductVariety.Product.Unit,
                last?.CheckedAt, last?.PricePerKg);
        }).ToList();

        return Json(new UrlReportResponse(
            DateTime.UtcNow,
            rows.Count(r => r.Status == "OkToday"),
            rows.Count(r => r.Status == "UrlExistsNoPrice"),
            rows.Count(r => r.Status == "NoUrl"),
            rows.Count(r => r.Status == "MarketInactive"),
            rows.Count(r => r.Status == "OutOfStock"),
            rows));
    }

    // Extractor Health Dashboard
    [HttpGet]
    public async Task<IActionResult> Health()
    {
        var data = await _healthService.GetHealthAsync();
        return View(data);
    }

    // Direct URL Discovery
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DiscoverDirectUrlCandidates(
        [FromBody] DiscoverCandidatesRequest req)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var candidates = await _discoveryService.DiscoverAsync(
            req.MarketId, req.ProductName, req.VarietyName, req.Unit, cts.Token);
        return Json(candidates);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
