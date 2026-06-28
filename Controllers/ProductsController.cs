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

    // ── URL YÖNETİMİ ──────────────────────────────────────────────────────────

    public async Task<IActionResult> Links(int id)
    {
        var product = await dbContext.Products
            .Include(p => p.Varieties.OrderBy(v => v.Id))
                .ThenInclude(v => v.DirectLinks)
                    .ThenInclude(l => l.Market)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLink(int productId, int varietyId, string url)
    {
        url = (url ?? "").Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            TempData["Error"] = "Geçersiz URL. https:// ile başlamalıdır.";
            return RedirectToAction(nameof(Links), new { id = productId });
        }

        var cleanUrl = CleanUrl(uri);

        var market = await FindOrCreateMarketAsync(uri);

        var exists = await dbContext.ProductMarketLinks
            .AnyAsync(l => l.ProductVarietyId == varietyId && l.DirectUrl == cleanUrl);

        if (!exists)
        {
            dbContext.ProductMarketLinks.Add(new ProductMarketLink
            {
                ProductVarietyId = varietyId,
                MarketId         = market.Id,
                DirectUrl        = cleanUrl,
                IsActive         = true,
            });
            await dbContext.SaveChangesAsync();
            TempData["Message"] = $"URL eklendi → {market.Name}";
        }
        else
        {
            TempData["Message"] = "Bu URL zaten kayıtlı.";
        }

        return RedirectToAction(nameof(Links), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLink(int productId, int linkId)
    {
        var link = await dbContext.ProductMarketLinks.FindAsync(linkId);
        if (link is not null)
        {
            dbContext.ProductMarketLinks.Remove(link);
            await dbContext.SaveChangesAsync();
            TempData["Message"] = "URL silindi.";
        }

        return RedirectToAction(nameof(Links), new { id = productId });
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    private async Task<Market> FindOrCreateMarketAsync(Uri uri)
    {
        var host = uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);

        // Mevcut market: BaseUrl içinde domain eşleşiyor mu?
        var markets = await dbContext.Markets.ToListAsync();
        var existing = markets.FirstOrDefault(m =>
            m.BaseUrl != null &&
            Uri.TryCreate(m.BaseUrl, UriKind.Absolute, out var mUri) &&
            mUri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase)
                .Equals(host, StringComparison.OrdinalIgnoreCase));

        if (existing is not null) return existing;

        // Yeni market oluştur
        var marketName = BuildMarketName(host);
        var newMarket = new Market
        {
            Name     = marketName,
            BaseUrl  = $"{uri.Scheme}://{uri.Host}",
            IsActive = true,
        };
        dbContext.Markets.Add(newMarket);
        await dbContext.SaveChangesAsync();
        return newMarket;
    }

    private static string BuildMarketName(string host)
    {
        // "portakalbahcem.com.tr" → "Portakalbahcem"
        var name = host
            .Replace(".com.tr", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".com",    "", StringComparison.OrdinalIgnoreCase)
            .Replace(".net",    "", StringComparison.OrdinalIgnoreCase)
            .Replace(".org",    "", StringComparison.OrdinalIgnoreCase)
            .Replace("-",       " ");

        return name.Length == 0 ? host
             : char.ToUpper(name[0]) + name.Substring(1);
    }

    private static string CleanUrl(Uri uri)
    {
        // PHP tabanlı siteler query string ile ürünü tanımlar
        if (uri.AbsolutePath.EndsWith(".php", StringComparison.OrdinalIgnoreCase))
            return uri.ToString();

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }
}
