using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Controllers;

public class ProductsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var products = await dbContext.Products
            .Include(product => product.Varieties)
            .OrderBy(product => product.Name)
            .ToListAsync();

        return View(products);
    }

    public IActionResult Create()
    {
        return View(new Product { Category = "Narenciye", Unit = "Kg", IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        product.CreatedAt = DateTime.UtcNow;
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        TempData["Message"] = "Urun kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await dbContext.Products.FindAsync(id);
        return product is null ? NotFound() : View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        dbContext.Update(product);
        await dbContext.SaveChangesAsync();

        TempData["Message"] = "Urun guncellendi.";
        return RedirectToAction(nameof(Index));
    }
}
