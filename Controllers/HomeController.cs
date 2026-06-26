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
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _dbContext.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var now        = DateTime.UtcNow;
        var cutoff60   = now.AddDays(-60);
        var cutoff30   = now.AddDays(-30);
        var cutoff7    = now.AddDays(-7);

        var allRecords = await _dbContext.PriceRecords
            .Include(r => r.ProductVariety)
            .Include(r => r.Market)
            .Where(r => r.IsLive && r.CheckedAt >= cutoff60)
            .OrderByDescending(r => r.CheckedAt)
            .ToListAsync();

        var summaries = products
            .Select(product =>
            {
                var prodRecs = allRecords.Where(r => r.ProductId == product.Id).ToList();

                var currentPrices = prodRecs
                    .Where(r => r.CheckedAt >= cutoff30)
                    .GroupBy(r => r.MarketId)
                    .Select(g => g.First())
                    .OrderBy(r => r.PricePerKg)
                    .ToList();

                if (currentPrices.Count == 0) return null;

                decimal? trendPct = null;
                var prevRecs = prodRecs.Where(r => r.CheckedAt < cutoff7).ToList();
                if (prevRecs.Count > 0)
                {
                    var curBest  = currentPrices[0].PricePerKg;
                    var prevBest = prevRecs.Min(r => r.PricePerKg);
                    if (prevBest > 0)
                        trendPct = Math.Round((curBest - prevBest) / prevBest * 100m, 1);
                }

                return new ProductPriceSummary
                {
                    Product     = product,
                    Prices      = currentPrices,
                    LastChecked = currentPrices.Max(r => r.CheckedAt),
                    TrendPct    = trendPct,
                };
            })
            .Where(s => s is not null && s.Prices.Count > 0)
            .Cast<ProductPriceSummary>()
            .OrderBy(s => s.Cheapest!.PricePerKg)
            .ToList();

        var model = new DashboardViewModel
        {
            ProductCount = products.Count,
            MarketCount = await _dbContext.Markets.CountAsync(m => m.IsActive),
            PriceRecordCount = await _dbContext.PriceRecords.CountAsync(),
            ProductSummaries = summaries
        };

        return View(model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
