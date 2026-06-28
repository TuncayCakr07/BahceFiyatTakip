using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace BahceFiyatTakip.Services.MarketPrices.Extractors;

/// <summary>
/// WooCommerce tabanlı sitelerden fiyat çıkarır.
/// Hem ürün detay sayfasını hem de arama sonuç sayfasını destekler.
/// </summary>
public static partial class WooCommercePriceExtractor
{
    public static bool IsMatch(string html) =>
        html.Contains("woocommerce-Price-amount", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Eşleşme yoksa null, hata/boşsa boş liste döner.
    /// </summary>
    public static List<PriceCandidate>? TryExtract(string html, string sourceUrl, string? baseUrl)
    {
        if (!IsMatch(html)) return null;
        try
        {
            return ExtractInternal(html, sourceUrl, baseUrl);
        }
        catch
        {
            return null;
        }
    }

    private static List<PriceCandidate> ExtractInternal(string html, string sourceUrl, string? baseUrl)
    {
        var results = new List<PriceCandidate>();

        // ── Ürün detay sayfası: h1.product_title var ──────────────────────────
        var titleMatch = ProductTitleRegex().Match(html);
        if (titleMatch.Success)
        {
            var name = WebUtility.HtmlDecode(titleMatch.Groups["n"].Value.Trim());

            var priceM = BdiPriceRegex().Match(html);
            if (priceM.Success && TryParseTrPrice(priceM.Groups["p"].Value, out var price))
            {
                // Stok: in-stock sınıfı varsa stokta var; out-of-stock varsa yok
                bool inStock = html.Contains("in-stock",    StringComparison.OrdinalIgnoreCase)
                            && !html.Contains("out-of-stock", StringComparison.OrdinalIgnoreCase);

                var img = ExtractWpPostImage(html, baseUrl);
                results.Add(new PriceCandidate(name, price, sourceUrl, img, inStock));
            }

            return results;  // Ürün detay sayfası: tek aday yeter
        }

        // ── Arama sonuç sayfası: product li öğeleri ───────────────────────────
        // HTML'i ürün blokları (<li class="product ...">...</li>) olarak böl.
        // Basit split: "class=" içinde "product" geçen li başlangıçlarına göre.
        var sections = ProductLiSplitRegex().Split(html);

        foreach (var section in sections.Skip(1))   // İlk parça product öncesi
        {
            var nameM = LoopTitleRegex().Match(section);
            if (!nameM.Success) continue;
            var name = WebUtility.HtmlDecode(nameM.Groups["n"].Value.Trim());

            var priceM = BdiPriceRegex().Match(section);
            if (!priceM.Success) continue;
            if (!TryParseTrPrice(priceM.Groups["p"].Value, out var price)) continue;

            var linkM = ProductLinkRegex().Match(section);
            var url   = linkM.Success
                ? Resolve(WebUtility.HtmlDecode(linkM.Groups["u"].Value), baseUrl) ?? sourceUrl
                : sourceUrl;

            var imgM    = ProductImageRegex().Match(section);
            var imgUrl  = imgM.Success ? Resolve(WebUtility.HtmlDecode(imgM.Groups["u"].Value), baseUrl) : null;

            // outofstock sınıfı WooCommerce stok-dışı ürünleri belirtir
            bool inStock = !section.Contains("outofstock", StringComparison.OrdinalIgnoreCase);

            results.Add(new PriceCandidate(name, price, url, imgUrl, inStock));
        }

        return results;
    }

    // ── Image helper ─────────────────────────────────────────────────────────

    private static string? ExtractWpPostImage(string html, string? baseUrl)
    {
        var m = WpPostImageRegex().Match(html);
        if (!m.Success) return null;
        return Resolve(WebUtility.HtmlDecode(m.Groups["u"].Value), baseUrl);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryParseTrPrice(string raw, out decimal price)
    {
        // "<bdi>150,00" → "150.00"
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

    // Ürün detay: <h1 class="product_title entry-title">Avokado Hass</h1>
    [GeneratedRegex(@"<h1[^>]+class=[""'][^""']*product_title[^""']*[""'][^>]*>(?<n>[^<]+)<", RegexOptions.IgnoreCase)]
    private static partial Regex ProductTitleRegex();

    // Arama sonucu başlığı: <h2 class="woocommerce-loop-product__title">Avokado</h2>
    [GeneratedRegex(@"class=[""'][^""']*woocommerce-loop-product__title[^""']*[""'][^>]*>(?<n>[^<]+)<", RegexOptions.IgnoreCase)]
    private static partial Regex LoopTitleRegex();

    // Fiyat: <bdi>150,00<span...>₺</span></bdi>  → "150,00"
    [GeneratedRegex(@"<bdi>\s*(?<p>[\d.,]+)", RegexOptions.IgnoreCase)]
    private static partial Regex BdiPriceRegex();

    // Ürün linki: <a class="...woocommerce-loop-product__link..." href="...">
    [GeneratedRegex(@"<a[^>]+class=[""'][^""']*woocommerce-loop-product__link[^""']*[""'][^>]+href=[""'](?<u>[^""']+)[""']|href=[""'](?<u>[^""']+)[""'][^>]+class=[""'][^""']*woocommerce-loop-product__link[^""']*[""']", RegexOptions.IgnoreCase)]
    private static partial Regex ProductLinkRegex();

    // Ürün görseli: <img class="...wp-post-image..." src="...">
    [GeneratedRegex(@"<img[^>]+class=[""'][^""']*wp-post-image[^""']*[""'][^>]+src=[""'](?<u>[^""']+)[""']|src=[""'](?<u>[^""']+)[""'][^>]+class=[""'][^""']*wp-post-image[^""']*[""']", RegexOptions.IgnoreCase)]
    private static partial Regex ProductImageRegex();

    // Detay sayfası görseli
    [GeneratedRegex(@"<img[^>]+class=[""'][^""']*wp-post-image[^""']*[""'][^>]+src=[""'](?<u>[^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex WpPostImageRegex();

    // Arama sonuç sayfasındaki ürün bloklarını bölmek için (li.product başlangıçları)
    [GeneratedRegex(@"<li[^>]+class=[""'][^""']*\bproduct\b[^""']*[""']", RegexOptions.IgnoreCase)]
    private static partial Regex ProductLiSplitRegex();
}
