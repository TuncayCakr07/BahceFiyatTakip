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
        var model = new DashboardViewModel
        {
            ProductCount = await _dbContext.Products.CountAsync(product => product.IsActive),
            MarketCount = await _dbContext.Markets.CountAsync(market => market.IsActive),
            PriceRecordCount = await _dbContext.PriceRecords.CountAsync(),
            LatestPrices = await _dbContext.PriceRecords
                .Include(record => record.Product)
                .Include(record => record.ProductVariety)
                .Include(record => record.Market)
                .OrderByDescending(record => record.CheckedAt)
                .Take(10)
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
