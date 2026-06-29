using BahceFiyatTakip.Data;
using BahceFiyatTakip.Services.MarketPrices;
using BahceFiyatTakip.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Services;

public class DirectUrlDiscoveryService(
    LiveMarketPriceProvider liveProvider,
    ApplicationDbContext dbContext,
    ILogger<DirectUrlDiscoveryService> logger)
{
    private static readonly System.Globalization.CultureInfo TrCulture =
        System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    public async Task<List<UrlCandidate>> DiscoverAsync(
        int marketId, string productName, string varietyName, string unit,
        CancellationToken ct = default)
    {
        var market = await dbContext.Markets
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == marketId, ct);

        var results = new List<UrlCandidate>();

        if (market is not null && !string.IsNullOrWhiteSpace(market.SearchUrlTemplate))
        {
            var query = BuildQuery(productName, varietyName, unit);
            try
            {
                var links = await liveProvider.FetchSearchLinksAsync(market, query, ct);
                foreach (var (title, url, _, hasPrice) in links)
                {
                    var c = ScoreCandidate(title, url, market.BaseUrl,
                        productName, varietyName, unit, hasPrice);
                    if (c.Score > 0 || results.Count < 3)
                        results.Add(c);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "URL keşfi başarısız: marketId={Id}", marketId);
            }
        }

        // Google arama linki — her zaman eklenir, scraping yapılmaz
        var marketName = market?.Name ?? "";
        var googleParts = new[] { marketName, productName, varietyName, unit }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var googleQ = Uri.EscapeDataString(string.Join(" ", googleParts));
        results.Add(new UrlCandidate(
            $"{productName} {varietyName} — Google'da Ara",
            $"https://www.google.com/search?q={googleQ}",
            "Google",
            0,
            "Google'da manuel arama için link (otomatik scraping yapılmadı)"
        ));

        return results
            .OrderByDescending(c => c.Score)
            .Take(8)
            .ToList();
    }

    // Spec: +40 ürün adı, +30 çeşit, +10 birim, +20 market domaini, +30 fiyat/stok bulundu
    private static UrlCandidate ScoreCandidate(
        string title, string url, string? baseUrl,
        string productName, string varietyName, string unit, bool hasPrice)
    {
        var prodNorm  = N(productName);
        var varNorm   = N(varietyName);
        var unitNorm  = N(unit);
        var urlNorm   = url.ToLowerInvariant();
        var titleNorm = N(title);
        var hay       = urlNorm + " " + titleNorm;

        var reasons = new List<string>();
        int score = 0;

        if (!string.IsNullOrEmpty(prodNorm) && ContainsTurkishWord(hay, prodNorm))
        {
            score += 40;
            reasons.Add("ürün adı eşleşti");
        }

        if (!string.IsNullOrEmpty(varNorm) && prodNorm != varNorm
            && ContainsTurkishWord(hay, varNorm))
        {
            score += 30;
            reasons.Add("çeşit eşleşti");
        }

        if (!string.IsNullOrEmpty(unitNorm) && hay.Contains(unitNorm))
        {
            score += 10;
            reasons.Add("birim eşleşti");
        }

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            try
            {
                var baseDomain = new Uri(baseUrl).Host;
                var urlHost    = new Uri(url).Host;
                if (urlHost.Equals(baseDomain, StringComparison.OrdinalIgnoreCase)
                    || urlHost.EndsWith("." + baseDomain, StringComparison.OrdinalIgnoreCase))
                {
                    score += 20;
                    reasons.Add("market domaini eşleşti");
                }
            }
            catch { }
        }

        if (hasPrice)
        {
            score += 30;
            reasons.Add("fiyat/stok bulundu");
        }

        var displayTitle = !string.IsNullOrWhiteSpace(title) ? title : TrimUrlPath(url);
        var reason = reasons.Count > 0 ? string.Join(", ", reasons) : "arama sonucu";
        return new UrlCandidate(displayTitle, url, "Market", Math.Clamp(score, 0, 100), reason);
    }

    // Türkçe ek toleranslı kelime eşleşmesi ("portakal" → "portakali" ✓)
    private static bool ContainsTurkishWord(string hay, string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        if (word.Length < 4) return hay.Contains(word, StringComparison.Ordinal);

        var idx = hay.IndexOf(word, StringComparison.Ordinal);
        while (idx >= 0)
        {
            bool startOk = idx == 0 || !char.IsLetter(hay[idx - 1]);
            if (startOk)
            {
                int after = idx + word.Length;
                if (after >= hay.Length || !char.IsLetter(hay[after])) return true;
                int suf = 0;
                while (after + suf < hay.Length && char.IsLetter(hay[after + suf]) && suf < 5) suf++;
                bool endOk = after + suf >= hay.Length || !char.IsLetter(hay[after + suf]);
                if (endOk && suf is >= 1 and <= 5) return true;
            }
            idx = hay.IndexOf(word, idx + 1, StringComparison.Ordinal);
        }
        return false;
    }

    private static string BuildQuery(string productName, string varietyName, string unit)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(productName)) parts.Add(productName.Trim());
        if (!string.IsNullOrWhiteSpace(varietyName)
            && !varietyName.Trim().Equals(productName.Trim(), StringComparison.OrdinalIgnoreCase))
            parts.Add(varietyName.Trim());
        if (!string.IsNullOrWhiteSpace(unit)
            && !unit.Equals("adet", StringComparison.OrdinalIgnoreCase)
            && !unit.Equals("kg", StringComparison.OrdinalIgnoreCase))
            parts.Add(unit.Trim());
        return string.Join(" ", parts);
    }

    private static string TrimUrlPath(string url)
    {
        try
        {
            var path = new Uri(url).AbsolutePath;
            return path.Length > 55 ? "…" + path[^52..] : path;
        }
        catch { return url.Length > 60 ? url[..57] + "…" : url; }
    }

    private static string N(string s) =>
        s.ToLower(TrCulture)
         .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
         .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
}
