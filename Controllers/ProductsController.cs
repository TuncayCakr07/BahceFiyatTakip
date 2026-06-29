using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.ViewModels;
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

    // ── DETAY SAYFASI ─────────────────────────────────────────────────────────

    public async Task<IActionResult> Details(int id)
    {
        var cutoff60 = DateTime.UtcNow.AddDays(-60);
        var cutoff14 = DateTime.UtcNow.AddDays(-14);

        var product = await dbContext.Products
            .Include(p => p.Varieties.Where(v => v.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        if (product is null) return NotFound();

        var prodRecs = await dbContext.PriceRecords
            .Include(r => r.Market)
            .Where(r => r.ProductId == id && r.IsLive && r.CheckedAt >= cutoff60 && r.InStock != false)
            .OrderByDescending(r => r.CheckedAt)
            .ToListAsync();

        var allVarietyIds = product.Varieties.Select(v => v.Id).ToList();
        var links = allVarietyIds.Count > 0
            ? await dbContext.ProductMarketLinks
                .Include(l => l.Market)
                .Where(l => l.IsActive && allVarietyIds.Contains(l.ProductVarietyId))
                .ToListAsync()
            : new List<ProductMarketLink>();

        var productMarkets = links
            .Select(l => l.Market)
            .DistinctBy(m => m.Id)
            .Select(m => new MarketColumn { Id = m.Id, Name = m.Name, BaseUrl = m.BaseUrl ?? "" })
            .OrderBy(m => m.IsSpecialty ? 0 : 1)
            .ThenBy(m => m.Name)
            .ToList();

        var activeVarieties = product.Varieties.Where(v => v.IsActive).OrderBy(v => v.Name).ToList();
        var hasVarietyTagged = prodRecs.Any(r => r.ProductVarietyId.HasValue);

        List<ProductVariety> varieties = (activeVarieties.Count == 0 || !hasVarietyTagged)
            ? [new ProductVariety { Id = 0, Name = "Genel", ProductId = product.Id }]
            : activeVarieties;

        var varietyRows = varieties.Select(variety =>
        {
            var allVarRecs = variety.Id == 0
                ? prodRecs
                : prodRecs.Where(r => r.ProductVarietyId == variety.Id).ToList();

            var varietyLinks = variety.Id == 0
                ? links
                : links.Where(l => l.ProductVarietyId == variety.Id).ToList();

            var linkedMarketIds  = varietyLinks.Select(l => l.MarketId).ToHashSet();
            var marketDirectUrls = varietyLinks
                .GroupBy(l => l.MarketId)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault(l => !string.IsNullOrEmpty(l.DirectUrl))?.DirectUrl ?? "");

            var marketPrices = productMarkets.ToDictionary(
                m => m.Id,
                m => (PriceRecord?)allVarRecs.Where(r => r.MarketId == m.Id).MaxBy(r => r.CheckedAt));

            var spark = allVarRecs
                .Where(r => r.CheckedAt >= cutoff14)
                .GroupBy(r => r.CheckedAt.ToLocalTime().Date)
                .OrderBy(g => g.Key)
                .Select(g => new SparkPoint(g.Key, g.Min(r => r.PricePerKg)))
                .ToList();

            decimal? trend = null;
            var recentBest = allVarRecs.Where(r => r.CheckedAt >= cutoff14).MinBy(r => r.PricePerKg);
            var olderBest  = allVarRecs.Where(r => r.CheckedAt < cutoff14).MinBy(r => r.PricePerKg);
            if (recentBest != null && olderBest != null && olderBest.PricePerKg > 0)
                trend = Math.Round((recentBest.PricePerKg - olderBest.PricePerKg) / olderBest.PricePerKg * 100m, 1);

            return new VarietyPriceRow
            {
                Variety          = variety,
                MarketPrices     = marketPrices,
                LinkedMarketIds  = linkedMarketIds,
                MarketDirectUrls = marketDirectUrls,
                SparklinePoints  = spark,
                TrendPct         = trend,
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
            .FirstOrDefault()?.ImageUrl ?? product.ImageUrl;

        var row = new ProductDashboardRow
        {
            Product          = product,
            ProductMarkets   = productMarkets,
            Varieties        = varietyRows,
            ProductSparkline = productSpark,
            BestImageUrl     = bestImg,
        };

        ViewData["Title"] = product.Name + " — Detay";
        return View(row);
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
