using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace BahceFiyatTakip.Services.MarketPrices.Extractors;

/// <summary>
/// NopCommerce tabanlı sitelerden fiyat çıkarır (örn. portakalbahcem.com).
/// Playwright ile render edilmiş HTML üzerinde çalışır.
/// </summary>
public static partial class NopCommercePriceExtractor
{
    public static bool IsMatch(string html) =>
        html.Contains("nopCommerce",      StringComparison.OrdinalIgnoreCase) ||
        html.Contains("stock-availability", StringComparison.Ordinal);

    /// <summary>
    /// Eşleşme yoksa null, hata/boşsa boş liste döner.
    /// </summary>
    public static List<PriceCandidate>? TryExtract(string html, string sourceUrl, string? baseUrl)
    {
        if (!IsMatch(html)) return null;
        try
        {
            var name  = ExtractName(html);
            var price = ExtractPrice(html);
            if (name is null || price is null) return null;

            var inStock  = ExtractInStock(html);
            var imageUrl = ExtractImage(html, baseUrl);

            return [new PriceCandidate(name, price.Value, sourceUrl, imageUrl, inStock)];
        }
        catch
        {
            return null;
        }
    }

    // ── Name ─────────────────────────────────────────────────────────────────

    private static string? ExtractName(string html)
    {
        // <h1 class="page-title ..." itemprop="name">Ürün Adı</h1>
        var m = H1ItempropNameRegex().Match(html);
        if (m.Success) return WebUtility.HtmlDecode(m.Groups["n"].Value.Trim());

        // Fallback: page-title class'ı olan herhangi bir h1
        m = H1PageTitleRegex().Match(html);
        return m.Success ? WebUtility.HtmlDecode(m.Groups["n"].Value.Trim()) : null;
    }

    // ── Price ─────────────────────────────────────────────────────────────────

    private static decimal? ExtractPrice(string html)
    {
        decimal p;

        // 1. Playwright render sonrası actual-price span: <span class="...actual-price...">375,00 TL</span>
        var m = ActualPriceRegex().Match(html);
        if (m.Success && TryParseTrPrice(m.Groups["p"].Value, out p)) return p;

        // 2. Schema.org content attribute: itemprop="price" content="375"
        m = ItemPropContentRegex().Match(html);
        if (m.Success && TryParseTrPrice(m.Groups["p"].Value, out p)) return p;

        // 3. Portakalbahcem özel JS değişkeni: indirimli_fiyati: "135,00"
        m = IndirimliRegex().Match(html);
        if (m.Success && TryParseTrPrice(m.Groups["p"].Value, out p) && p > 0) return p;

        // 4. Temel fiyat JS değişkeni: urun_fiyati: "375,00"
        m = UrunFiyatRegex().Match(html);
        if (m.Success && TryParseTrPrice(m.Groups["p"].Value, out p) && p > 0) return p;

        return null;
    }

    // ── Stock ─────────────────────────────────────────────────────────────────

    private static bool ExtractInStock(string html)
    {
        var m = StockAvailabilityRegex().Match(html);
        if (!m.Success) return false;
        var text = m.Groups["s"].Value.Trim();
        return text.Contains("Var",    StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Stokta", StringComparison.OrdinalIgnoreCase);
    }

    // ── Image ─────────────────────────────────────────────────────────────────

    private static string? ExtractImage(string html, string? baseUrl)
    {
        var m = ItempropImageRegex().Match(html);
        if (!m.Success) return null;
        return Resolve(WebUtility.HtmlDecode(m.Groups["u"].Value.Trim()), baseUrl);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryParseTrPrice(string raw, out decimal price)
    {
        // "375,00 TL" | "375,00" | "375" → 375.00
        var s = raw.Replace("TL", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("₺", "").Replace(" ", "")
                   .Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out price) && price > 0)
            return true;
        price = 0;
        return false;
    }

    private static string? Resolve(string? url, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;
        if (!string.IsNullOrWhiteSpace(baseUrl) &&
            Uri.TryCreate(new Uri(baseUrl), url, out var abs)) return abs.ToString();
        return null;
    }

    // ── Regexes ───────────────────────────────────────────────────────────────

    // <h1 class="page-title ... " itemprop="name">Ürün Adı</h1>
    [GeneratedRegex(@"<h1[^>]+itemprop=[""']name[""'][^>]*>(?<n>[^<]+)<", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex H1ItempropNameRegex();

    [GeneratedRegex(@"<h1[^>]+class=[""'][^""']*page-title[^""']*[""'][^>]*>(?<n>[^<]+)<", RegexOptions.IgnoreCase)]
    private static partial Regex H1PageTitleRegex();

    // <span class="...actual-price...">375,00 TL</span>
    [GeneratedRegex(@"<span[^>]+class=[""'][^""']*\bactual-price\b[^""']*[""'][^>]*>\s*(?<p>[\d.,]+)\s*(?:TL|₺)?", RegexOptions.IgnoreCase)]
    private static partial Regex ActualPriceRegex();

    // itemprop="price" content="375"  veya  content="375" itemprop="price"
    [GeneratedRegex(
        @"itemprop=[""']price[""'][^>]+content=[""'](?<p>[\d.,]+)[""']|content=[""'](?<p>[\d.,]+)[""'][^>]+itemprop=[""']price[""']",
        RegexOptions.IgnoreCase)]
    private static partial Regex ItemPropContentRegex();

    // Portakalbahcem özel: indirimli_fiyati: "135,00"
    [GeneratedRegex(@"indirimli_fiyati\s*:\s*[""'](?<p>[\d.,]+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex IndirimliRegex();

    // Portakalbahcem özel: urun_fiyati: "375,00"
    [GeneratedRegex(@"urun_fiyati\s*:\s*[""'](?<p>[\d.,]+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex UrunFiyatRegex();

    // <span class="stock-availability">Stokta Var</span>
    [GeneratedRegex(@"<span[^>]+class=[""']stock-availability[""'][^>]*>(?<s>[^<]+)<", RegexOptions.IgnoreCase)]
    private static partial Regex StockAvailabilityRegex();

    // <img itemprop="image" src="...">  veya  <img src="..." itemprop="image">
    [GeneratedRegex(
        @"<img[^>]+itemprop=[""']image[""'][^>]+(?:src|data-src)=[""'](?<u>[^""']+)[""']|<img[^>]+(?:src|data-src)=[""'](?<u>[^""']+)[""'][^>]+itemprop=[""']image[""']",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ItempropImageRegex();
}
