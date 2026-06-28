using BahceFiyatTakip.Data;
using BahceFiyatTakip.Services;
using BahceFiyatTakip.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Controllers;

public class PriceChecksController(
    ApplicationDbContext dbContext,
    IPriceTrackingService priceTrackingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        await PopulateProductsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Check(int productId)
    {
        var savedRecords = await priceTrackingService.CheckAndSavePricesAsync(productId);

        TempData["Message"] = savedRecords.Count > 0
            ? $"{savedRecords.Count} marketten fiyat güncellendi."
            : "Bu ürün için şu an fiyat bulunamadı.";

        return RedirectToAction("Index", "Home");
    }

    // AJAX endpoint — JSON döner, redirect yapmaz
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckApi(int productId)
    {
        try
        {
            var saved = await priceTrackingService.CheckAndSavePricesAsync(productId);
            return Json(new { ok = true, count = saved.Count });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, count = 0, error = ex.Message });
        }
    }

    public async Task<IActionResult> History(int? productId, int? marketId, DateTime? startDate, DateTime? endDate)
    {
        var query = dbContext.PriceRecords
            .Include(record => record.Product)
            .Include(record => record.ProductVariety)
            .Include(record => record.Market)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(record => record.ProductId == productId);
        }

        if (marketId.HasValue)
        {
            query = query.Where(record => record.MarketId == marketId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(record => record.CheckedAt >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(record => record.CheckedAt < endDate.Value.Date.AddDays(1));
        }

        var model = new PriceHistoryViewModel
        {
            ProductId = productId,
            MarketId = marketId,
            StartDate = startDate,
            EndDate = endDate,
            Products = await dbContext.Products.OrderBy(product => product.Name).ToListAsync(),
            Markets = await dbContext.Markets
                .Where(market => market.IsActive)
                .OrderBy(market => market.Name)
                .ToListAsync(),
            Records = await query
                .OrderByDescending(record => record.CheckedAt)
                .ThenBy(record => record.Market.Name)
                .Take(500)
                .ToListAsync()
        };

        return View(model);
    }

    private async Task PopulateProductsAsync()
    {
        var products = await dbContext.Products
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ToListAsync();

        ViewBag.Products = new SelectList(products, "Id", "Name");
    }
}
