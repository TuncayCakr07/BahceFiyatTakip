using BahceFiyatTakip.Data;
using BahceFiyatTakip.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace BahceFiyatTakip.Services.ExcelSeed;

public record SeedResult(int Products, int Varieties, int Markets, int Links, int Aliases);

public class ExcelSeedService(ApplicationDbContext db, ILogger<ExcelSeedService> logger)
{
    private record ExcelRow(string ProductName, string VarietyName, MarketInfo Market);

    public async Task<SeedResult> SeedAsync(string excelPath, CancellationToken ct = default)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var rows = ParseExcel(excelPath);
        if (rows.Count == 0)
        {
            logger.LogWarning("ExcelSeedService: {Path} dosyasından hiç satır okunamadı", excelPath);
            return new SeedResult(0, 0, 0, 0, 0);
        }
        logger.LogInformation("ExcelSeedService: {Count} (ürün,çeşit,market) satırı okundu", rows.Count);

        int addedProducts = 0, addedMarkets = 0, addedVarieties = 0, addedLinks = 0, addedAliases = 0;

        // ── Adım 1: Products ────────────────────────────────────────────────────
        var productsByName = (await db.Products.ToListAsync(ct))
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in rows.Select(r => r.ProductName)
                                 .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (productsByName.ContainsKey(name)) continue;
            var p = new Product
            {
                Name     = name,
                Category = GetProductCategory(name),
                Unit     = GetProductUnit(name),
                IsActive = true
            };
            db.Products.Add(p);
            productsByName[name] = p;
            addedProducts++;
        }
        await db.SaveChangesAsync(ct);

        // ── Adım 2: Markets ─────────────────────────────────────────────────────
        var marketsByName = (await db.Markets.ToListAsync(ct))
            .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

        var distinctMarkets = rows
            .GroupBy(r => r.Market.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First().Market);

        foreach (var info in distinctMarkets)
        {
            if (marketsByName.TryGetValue(info.Name, out var existing))
            {
                bool changed = false;
                if (info.BaseUrl is not null && existing.BaseUrl is null)
                { existing.BaseUrl = info.BaseUrl; changed = true; }
                if (info.SearchUrlTemplate is not null && existing.SearchUrlTemplate is null)
                { existing.SearchUrlTemplate = info.SearchUrlTemplate; changed = true; }
                if (changed) db.Markets.Update(existing);
                continue;
            }
            var m = new Market
            {
                Name              = info.Name,
                BaseUrl           = info.BaseUrl,
                SearchUrlTemplate = info.SearchUrlTemplate,
                IsActive          = info.IsActive
            };
            db.Markets.Add(m);
            marketsByName[m.Name] = m;
            addedMarkets++;
        }
        await db.SaveChangesAsync(ct);

        // ── Adım 3: ProductVarieties ────────────────────────────────────────────
        static string VKey(int productId, string name) =>
            $"{productId}|{name.ToLowerInvariant()}";

        var varietiesByKey = (await db.ProductVarieties.ToListAsync(ct))
            .ToDictionary(v => VKey(v.ProductId, v.Name));

        var uniqueVarieties = rows
            .GroupBy(r => (r.ProductName.ToLowerInvariant(), r.VarietyName.ToLowerInvariant()))
            .Select(g => g.First());

        foreach (var row in uniqueVarieties)
        {
            if (!productsByName.TryGetValue(row.ProductName, out var product)) continue;
            var key = VKey(product.Id, row.VarietyName);
            if (varietiesByKey.ContainsKey(key)) continue;

            var v = new ProductVariety
            {
                ProductId = product.Id,
                Name      = row.VarietyName,
                IsActive  = true
            };
            db.ProductVarieties.Add(v);
            varietiesByKey[key] = v;
            addedVarieties++;
        }
        await db.SaveChangesAsync(ct);

        // ── Adım 4: ProductMarketLinks ──────────────────────────────────────────
        var existingLinks = (await db.ProductMarketLinks.ToListAsync(ct))
            .Select(l => (l.ProductVarietyId, l.MarketId, l.DirectUrl))
            .ToHashSet();

        foreach (var row in rows)
        {
            if (!productsByName.TryGetValue(row.ProductName, out var product)) continue;
            if (!marketsByName.TryGetValue(row.Market.Name, out var market)) continue;
            var key = VKey(product.Id, row.VarietyName);
            if (!varietiesByKey.TryGetValue(key, out var variety)) continue;

            var triple = (variety.Id, market.Id, "");
            if (existingLinks.Contains(triple)) continue;

            db.ProductMarketLinks.Add(new ProductMarketLink
            {
                ProductVarietyId = variety.Id,
                MarketId         = market.Id,
                DirectUrl        = "",
                IsActive         = true
            });
            existingLinks.Add(triple);
            addedLinks++;
        }
        await db.SaveChangesAsync(ct);

        // ── Adım 5: ProductSearchAliases ────────────────────────────────────────
        var existingAliases = (await db.ProductSearchAliases.ToListAsync(ct))
            .Select(a => (a.ProductVarietyId, a.Query))
            .ToHashSet();

        foreach (var (productName, varietyName, _) in rows
            .GroupBy(r => (r.ProductName.ToLowerInvariant(), r.VarietyName.ToLowerInvariant()))
            .Select(g => g.First()))
        {
            if (!productsByName.TryGetValue(productName, out var product)) continue;
            var key = VKey(product.Id, varietyName);
            if (!varietiesByKey.TryGetValue(key, out var variety)) continue;

            var alias = GenerateAlias(varietyName);
            if (string.IsNullOrWhiteSpace(alias)) continue;

            var aliasKey = (variety.Id, alias);
            if (existingAliases.Contains(aliasKey)) continue;

            db.ProductSearchAliases.Add(new ProductSearchAlias
            {
                ProductVarietyId = variety.Id,
                Query            = alias,
                Priority         = 100
            });
            existingAliases.Add(aliasKey);
            addedAliases++;
        }
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "ExcelSeedService tamamlandı: +{P} ürün, +{V} çeşit, +{M} market, +{L} link, +{A} alias",
            addedProducts, addedVarieties, addedMarkets, addedLinks, addedAliases);

        return new SeedResult(addedProducts, addedVarieties, addedMarkets, addedLinks, addedAliases);
    }

    // ── Excel Ayrıştırma ────────────────────────────────────────────────────────

    private static List<ExcelRow> ParseExcel(string path)
    {
        var result = new List<ExcelRow>();
        using var pkg = new ExcelPackage(new FileInfo(path));

        foreach (var ws in pkg.Workbook.Worksheets)
        {
            if (ws.Dimension == null || ws.Dimension.Rows < 2) continue;

            int totalRows = ws.Dimension.Rows;

            // Col 3 header "Gr" veya "GRAMAJ" → satır başına gramaj sütunu var
            var col3Header = (ws.Cells[1, 3].Text ?? "").Trim();
            bool hasGramaj = col3Header.Equals("Gr", StringComparison.OrdinalIgnoreCase) ||
                             col3Header.Equals("GRAMAJ", StringComparison.OrdinalIgnoreCase);

            string lastVarietyRaw = "";

            for (int r = 2; r <= totalRows; r++)
            {
                var col1 = (ws.Cells[r, 1].Text ?? "").Trim();
                var col2 = (ws.Cells[r, 2].Text ?? "").Trim();

                // Market sütunu boşsa → grup başlığı veya boş satır, geç
                if (string.IsNullOrWhiteSpace(col2)) continue;

                // Col1 doluysa çeşit adını güncelle (boşsa önceki adı taşı)
                if (!string.IsNullOrWhiteSpace(col1)) lastVarietyRaw = col1;

                if (string.IsNullOrWhiteSpace(lastVarietyRaw)) continue;

                // Gramaj varsa çeşit adına ekle
                string varietyName = lastVarietyRaw;
                if (hasGramaj)
                {
                    var gramaj = (ws.Cells[r, 3].Text ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(gramaj))
                        varietyName = $"{lastVarietyRaw} {gramaj} Gr";
                }

                if (varietyName.Length > 99) varietyName = varietyName[..99];

                var market = MarketNormalizer.Normalize(col2);
                var productName = GetProductName(ws.Name, varietyName);

                result.Add(new ExcelRow(productName, varietyName, market));
            }
        }

        return result;
    }

    // ── Sayfa adı → Ürün adı ────────────────────────────────────────────────────

    private static string GetProductName(string sheetName, string varietyName)
    {
        var s = sheetName.Trim();

        if (s.Contains("Portakal", StringComparison.OrdinalIgnoreCase)) return "Portakal";
        if (s.Contains("Limon Otu", StringComparison.OrdinalIgnoreCase)) return "Limon Otu";
        if (s.Contains("Limon", StringComparison.OrdinalIgnoreCase)) return "Limon";
        if (s.Contains("Mandalina", StringComparison.OrdinalIgnoreCase)) return "Mandalina";
        if (s.Contains("Hicaz Nar", StringComparison.OrdinalIgnoreCase)) return "Nar";
        if (s.Contains("Avokado", StringComparison.OrdinalIgnoreCase)) return "Avokado";
        if (s.Contains("Kumkuat", StringComparison.OrdinalIgnoreCase) || s.Contains("Limekuat", StringComparison.OrdinalIgnoreCase))
            return varietyName.Contains("Limekuat", StringComparison.OrdinalIgnoreCase) ? "Limekuat" : "Kumkuat";
        if (s.Contains("Şadok", StringComparison.OrdinalIgnoreCase)) return "Şadok";
        if (s.Contains("Bergamot", StringComparison.OrdinalIgnoreCase) || s.Contains("Turunç", StringComparison.OrdinalIgnoreCase))
            return varietyName.Contains("Bergamot", StringComparison.OrdinalIgnoreCase) ? "Bergamot" : "Turunç";
        if (s.Contains("Greyfurt", StringComparison.OrdinalIgnoreCase)) return "Greyfurt";
        if (s.Contains("Lime", StringComparison.OrdinalIgnoreCase))
            return varietyName.Contains("Finger", StringComparison.OrdinalIgnoreCase) ? "Finger Lime" : "Lime";
        if (s.Contains("Reçel", StringComparison.OrdinalIgnoreCase) || s.Contains("Ekş", StringComparison.OrdinalIgnoreCase))
            return varietyName.Contains("Ekş", StringComparison.OrdinalIgnoreCase) ? "Nar Ekşisi" : "Reçel & Marmelat";
        if (s.Contains("Kurutulmuş", StringComparison.OrdinalIgnoreCase)) return "Kurutulmuş Ürünler";
        if (s.Contains("Meyve Su", StringComparison.OrdinalIgnoreCase)) return "Meyve Suyu";
        if (s.Contains("Ejder", StringComparison.OrdinalIgnoreCase)) return "Ejder Meyvesi";
        if (s.Contains("Çarkıfelek", StringComparison.OrdinalIgnoreCase)) return "Çarkıfelek";
        if (s.Contains("Mango", StringComparison.OrdinalIgnoreCase)) return "Mango";
        if (s.Contains("Domates", StringComparison.OrdinalIgnoreCase)) return "Domates";

        return "Diğer Ürünler";
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────────────

    private static string GetProductCategory(string name) => name switch
    {
        "Portakal" or "Limon" or "Mandalina" or "Greyfurt" or "Kumkuat" or
        "Limekuat" or "Şadok" or "Turunç" or "Bergamot" or "Lime" or "Finger Lime" => "Narenciye",
        "Avokado" or "Ejder Meyvesi" or "Çarkıfelek" or "Mango" => "Tropikal",
        "Domates" => "Sebze",
        "Limon Otu" => "Diğer",
        "Nar" => "Meyve",
        "Reçel & Marmelat" or "Nar Ekşisi" or "Kurutulmuş Ürünler" or "Meyve Suyu" => "İşlenmiş",
        _ => "Diğer"
    };

    private static string GetProductUnit(string name) => name switch
    {
        "Reçel & Marmelat" or "Nar Ekşisi" => "Adet",
        "Meyve Suyu" => "Lt",
        _ => "Kg"
    };

    private static string GenerateAlias(string varietyName)
    {
        var lower = varietyName.ToLowerInvariant()
            .Replace('ğ', 'g')
            .Replace('ş', 's')
            .Replace('ı', 'i')
            .Replace('ö', 'o')
            .Replace('ü', 'u')
            .Replace('ç', 'c');
        return lower.Length > 150 ? lower[..150] : lower;
    }
}
