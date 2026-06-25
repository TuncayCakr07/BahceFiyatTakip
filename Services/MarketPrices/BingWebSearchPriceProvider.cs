using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

public partial class BingWebSearchPriceProvider(
    PlaywrightPageFetcher pageFetcher,
    HttpClient httpClient,
    ILogger<BingWebSearchPriceProvider> logger) : IMarketPriceProvider
{
    public string ProviderName => "BingWebSearch";

    public async Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(
        Product product,
        IReadOnlyList<Market> markets,
        CancellationToken cancellationToken = default)
    {
        if (!pageFetcher.IsAvailable) return [];

        var webMarket = markets.FirstOrDefault(m => m.Name == "Web Arama");
        if (webMarket is null) return [];

        var results = new List<MarketPriceResult>();

        foreach (var target in BuildSearchTargets(product).Take(2))
        {
            var query = $"{target.Query} fiyat kg -ml -şişe -kutu -suyu";
            var searchUrl = $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}&cc=TR&setlang=tr-TR&count=5";

            var links = await pageFetcher.GetLinksAsync(searchUrl, "li.b_algo h2 a[href]", cancellationToken);
            logger.LogInformation("BingSearch '{Q}' → {N} sonuç", target.Query, links.Count);

            foreach (var link in links.Take(5))
            {
                var result = await TryFetchPriceAsync(link, webMarket, target, cancellationToken);
                if (result is not null)
                {
                    results.Add(result);
                    logger.LogInformation("BingSearch: '{Title}' → {Price} TL @ {Url}", result.MatchedTitle, result.PricePerKg, link);
                    break;
                }
            }

            if (results.Count > 0) break;
        }

        return results;
    }

    private async Task<MarketPriceResult?> TryFetchPriceAsync(
        string url, Market webMarket, SearchTarget target, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            req.Headers.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9");

            using var resp = await httpClient.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;

            var html = await resp.Content.ReadAsStringAsync(ct);

            var price = ExtractPrice(html);
            if (price is null) return null;

            var score = CalcConfidence(html, target);
            if (score < 40) return null;

            return new MarketPriceResult(
                webMarket.Id, webMarket.Name,
                price.Value, url, ProviderName,
                IsLive: true,
                ProductVarietyId: target.ProductVarietyId,
                MatchedTitle: ExtractTitle(html) ?? target.DisplayName,
                ImageUrl: ExtractImageUrl(html),
                ConfidenceScore: score);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            logger.LogDebug(ex, "BingSearch: sayfa okunamadı: {Url}", url);
            return null;
        }
    }

    private static decimal? ExtractPrice(string html)
    {
        foreach (Match m in TurkishPriceRegex().Matches(html))
        {
            var s = m.Groups["p"].Value.Replace(".", "").Replace(',', '.');
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v is > 1 and < 5000)
                return v;
        }
        foreach (Match m in JsonPriceRegex().Matches(html))
        {
            var s = m.Groups["p"].Value.Replace(".", "").Replace(',', '.');
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v is > 1 and < 5000)
                return v;
        }
        return null;
    }

    private static string? ExtractTitle(string html)
    {
        var m = TitleRegex().Match(html);
        return m.Success ? WebUtility.HtmlDecode(m.Groups["t"].Value.Trim()) : null;
    }

    private static string? ExtractImageUrl(string html)
    {
        var m = ImageRegex().Match(html);
        return m.Success ? m.Groups["u"].Value : null;
    }

    private static int CalcConfidence(string html, SearchTarget target)
    {
        var n = Norm(html);
        var score = 20;
        if (n.Contains(Norm(target.ProductName))) score += 25;
        if (n.Contains(Norm(target.VarietyName))) score += 25;
        if (ContainsAny(n, "kg", "gram", "adet", "taze", "meyve", "sebze")) score += 15;
        if (ContainsAny(n, "fiyat", "sepete", "satin al", "ekle")) score += 15;
        return Math.Min(score, 100);
    }

    private static string Norm(string s) =>
        WebUtility.HtmlDecode(s).ToLowerInvariant()
            .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
            .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");

    private static bool ContainsAny(string s, params string[] needles) =>
        needles.Any(n => s.Contains(n, StringComparison.Ordinal));

    private static IReadOnlyList<SearchTarget> BuildSearchTargets(Product product)
    {
        var targets = product.Varieties
            .Where(v => v.IsActive)
            .Select(v =>
            {
                var q = v.SearchAliases.OrderBy(a => a.Priority).FirstOrDefault()?.Query
                        ?? $"{v.Name} {product.Name}".ToLowerInvariant();
                return new SearchTarget(v.Id, q, $"{v.Name} {product.Name}", v.Name, product.Name);
            })
            .ToList();

        return targets.Count > 0
            ? targets
            : [new SearchTarget(null, product.Name, product.Name, product.Name, product.Name)];
    }

    [GeneratedRegex(@"(?<p>\d{1,5}(?:[.,]\d{1,2})?)\s*(?:TL|TRY|₺)", RegexOptions.IgnoreCase)]
    private static partial Regex TurkishPriceRegex();

    [GeneratedRegex(@"""price""\s*:\s*""?(?<p>\d{1,5}(?:[.,]\d{1,2})?)""?", RegexOptions.IgnoreCase)]
    private static partial Regex JsonPriceRegex();

    [GeneratedRegex(@"<title[^>]*>(?<t>[^<]+)</title>", RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"(?:src|content)=""(?<u>https?://[^""]+?\.(?:jpg|jpeg|png|webp)(?:\?[^""]*)?)""\s", RegexOptions.IgnoreCase)]
    private static partial Regex ImageRegex();

    private sealed record SearchTarget(int? ProductVarietyId, string Query, string DisplayName, string VarietyName, string ProductName);
}
