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

        var cutoff = DateTime.UtcNow.AddDays(-30);
        var records = await _dbContext.PriceRecords
            .Include(r => r.ProductVariety)
            .Include(r => r.Market)
            .Where(r => r.IsLive && r.CheckedAt >= cutoff)
            .OrderByDescending(r => r.CheckedAt)
            .ToListAsync();

        var summaries = products.Select(product =>
        {
            var productRecords = records
                .Where(r => r.ProductId == product.Id)
                .GroupBy(r => r.MarketId)
                .Select(g => g.First())
                .OrderBy(r => r.PricePerKg)
                .ToList();

            return new ProductPriceSummary
            {
                Product = product,
                Prices = productRecords,
                LastChecked = productRecords.Count > 0 ? productRecords.Max(r => r.CheckedAt) : null
            };
        }).ToList();

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
