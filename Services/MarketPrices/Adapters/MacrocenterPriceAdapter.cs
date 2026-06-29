using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices.Normalization;
using BahceFiyatTakip.Services.MarketPrices.Validation;

namespace BahceFiyatTakip.Services.MarketPrices.Adapters;

/// <summary>
/// Macrocenter ürün sayfasındaki JSON-LD (schema.org Product) verisini parse eder.
/// Yalnızca DirectUrl ile çalışır; SearchUrlTemplate kullanmaz.
/// Birim normalizasyonu: UnitNormalizer — Doğrulama: ValidationGate.
/// </summary>
public partial class MacrocenterPriceAdapter(
    PlaywrightPageFetcher pageFetcher,
    ILogger<MacrocenterPriceAdapter> logger)
{
    private const string AdapterProvider = "MacrocenterLd";

    public async Task<MarketPriceResult?> TryFetchAsync(
        Market market,
        string directUrl,
        string productName,
        string varietyName,
        int?   varietyId,
        string productUnit,
        CancellationToken ct = default)
    {
        if (!pageFetcher.IsAvailable)
        {
            logger.LogDebug("MacrocenterAdapter: Playwright mevcut değil.");
            return null;
        }

        logger.LogInformation("MacrocenterAdapter: {Url}", directUrl);

        string? html;
        try
        {
            html = await pageFetcher.FetchProductPageAsync(directUrl, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning("MacrocenterAdapter: Playwright hatası — {Msg}", ex.Message);
            return null;
        }

        if (string.IsNullOrEmpty(html))
        {
            logger.LogInformation("MacrocenterAdapter: HTML alınamadı. {Url}", directUrl);
            return null;
        }

        foreach (Match block in LdJsonRegex().Matches(html))
        {
            var jsonText = WebUtility.HtmlDecode(block.Groups["json"].Value).Trim();
            if (string.IsNullOrEmpty(jsonText)) continue;

            var result = TryParseBlock(
                jsonText, market, directUrl,
                productName, varietyName, varietyId, productUnit);
            if (result is not null)
                return result;
        }

        logger.LogInformation("MacrocenterAdapter: JSON-LD Product bulunamadı — {Url}", directUrl);
        return null;
    }

    // ── JSON-LD parsing ──────────────────────────────────────────────────────

    private MarketPriceResult? TryParseBlock(
        string json,
        Market market, string directUrl,
        string productName, string varietyName, int? varietyId, string productUnit)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling     = JsonCommentHandling.Skip
                });
        }
        catch (JsonException) { return null; }

        using (doc)
        {
            var root = doc.RootElement;

            // Root array: [{"@type":"Product",...}, {"@type":"BreadcrumbList",...}]
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var node in root.EnumerateArray())
                {
                    var r = TryExtractProduct(node, market, directUrl,
                        productName, varietyName, varietyId, productUnit);
                    if (r is not null) return r;
                }
                return null;
            }

            // @graph wrapper
            if (root.TryGetProperty("@graph", out var graph) &&
                graph.ValueKind == JsonValueKind.Array)
            {
                foreach (var node in graph.EnumerateArray())
                {
                    var r = TryExtractProduct(node, market, directUrl,
                        productName, varietyName, varietyId, productUnit);
                    if (r is not null) return r;
                }
                return null;
            }

            return TryExtractProduct(root, market, directUrl,
                productName, varietyName, varietyId, productUnit);
        }
    }

    private MarketPriceResult? TryExtractProduct(
        JsonElement el,
        Market market, string directUrl,
        string productName, string varietyName, int? varietyId, string productUnit)
    {
        if (!IsProductType(el)) return null;

        var name = GetStr(el, "name");
        if (string.IsNullOrWhiteSpace(name)) return null;

        decimal price    = 0;
        bool?   inStock  = null;
        string? offerUrl = null;

        if (el.TryGetProperty("offers", out var offersEl))
        {
            var offerNode = offersEl.ValueKind == JsonValueKind.Array
                ? offersEl.EnumerateArray().FirstOrDefault()
                : offersEl;

            if (offerNode.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            {
                price    = ExtractPrice(offerNode);
                inStock  = ExtractInStock(offerNode);
                offerUrl = GetStr(offerNode, "url");
            }
        }

        // Fiyat offers içinde bulunamazsa root'ta dene
        if (price <= 0) price = ExtractPrice(el);

        var imageUrl = ExtractImageUrl(el);

        // ── Birim normalizasyonu (UnitNormalizer) ────────────────────────────

        var norm      = UnitNormalizer.Normalize(name, price, productUnit);
        var unitLabel = norm.IsReliable ? norm.NormalizedUnit : "bilinmiyor";

        if (!norm.IsReliable)
            logger.LogDebug(
                "MacrocenterAdapter: Birim güvenilir değil — {Reason}. '{Name}'",
                norm.RejectReason, name);

        // ── ValidationGate — tüm kurallar burada çalışır ────────────────────

        var vr = ValidationGate.Validate(new CandidateInput(
            name, norm.NormalizedPrice, unitLabel, inStock, productName, varietyName));

        if (!vr.IsValid)
        {
            logger.LogInformation(
                "MacrocenterAdapter: Ret [{Rule}] → {Reason}",
                vr.FailedRule, vr.RejectReason);
            return null;
        }

        var sourceUrl = !string.IsNullOrWhiteSpace(offerUrl) ? offerUrl : directUrl;

        logger.LogInformation(
            "MacrocenterAdapter: ✓ '{Name}' → {Price} TL/{Unit} (confidence:{Score}, inStock:{Stock})",
            name, norm.NormalizedPrice, unitLabel, vr.ConfidenceScore,
            inStock.HasValue ? inStock.Value.ToString() : "?");

        return new MarketPriceResult(
            market.Id,
            market.Name,
            norm.NormalizedPrice,
            sourceUrl,
            AdapterProvider,
            IsLive: true,
            ProductVarietyId: varietyId,
            MatchedTitle: name,
            ImageUrl: imageUrl,
            ConfidenceScore: vr.ConfidenceScore,
            InStock: inStock);   // null korunur — stok bilinmiyorsa true varsayılmaz
    }

    // ── Fiyat ────────────────────────────────────────────────────────────────

    private static decimal ExtractPrice(JsonElement el)
    {
        foreach (var key in new[] { "price", "lowPrice" })
        {
            if (!el.TryGetProperty(key, out var pEl)) continue;
            var v = ParseDecimal(pEl);
            if (v > 0) return v;
        }

        // priceSpecification fallback
        if (el.TryGetProperty("priceSpecification", out var spec))
        {
            var node = spec.ValueKind == JsonValueKind.Array
                ? spec.EnumerateArray().FirstOrDefault()
                : spec;
            if (node.ValueKind is not JsonValueKind.Undefined)
                if (node.TryGetProperty("price", out var pp))
                {
                    var v = ParseDecimal(pp);
                    if (v > 0) return v;
                }
        }

        return 0;
    }

    private static decimal ParseDecimal(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var n))
            return n;
        if (el.ValueKind == JsonValueKind.String)
        {
            var s = (el.GetString() ?? "")
                .Replace("TL",  "", StringComparison.OrdinalIgnoreCase)
                .Replace("₺",   "")
                .Replace(" ",   "")
                .Replace(",",   ".");
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && d > 0)
                return d;
        }
        return 0;
    }

    // ── Stok ─────────────────────────────────────────────────────────────────

    private static bool? ExtractInStock(JsonElement offer)
    {
        if (!offer.TryGetProperty("availability", out var avEl) ||
            avEl.ValueKind != JsonValueKind.String) return null;

        var av = avEl.GetString() ?? "";

        if (av.Contains("InStock",             StringComparison.OrdinalIgnoreCase) ||
            av.Contains("LimitedAvailability", StringComparison.OrdinalIgnoreCase) ||
            av.Contains("PreOrder",            StringComparison.OrdinalIgnoreCase))
            return true;

        if (av.Contains("OutOfStock",    StringComparison.OrdinalIgnoreCase) ||
            av.Contains("SoldOut",       StringComparison.OrdinalIgnoreCase) ||
            av.Contains("Discontinued",  StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }

    // ── Resim ─────────────────────────────────────────────────────────────────

    private static string? ExtractImageUrl(JsonElement el)
    {
        if (!el.TryGetProperty("image", out var img)) return null;
        if (img.ValueKind == JsonValueKind.String)
            return img.GetString();
        if (img.ValueKind == JsonValueKind.Array)
        {
            var first = img.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.String) return first.GetString();
            if (first.ValueKind == JsonValueKind.Object) return GetStr(first, "url", "contentUrl");
        }
        if (img.ValueKind == JsonValueKind.Object)
            return GetStr(img, "url", "contentUrl");
        return null;
    }

    // ── Yardımcı metodlar ────────────────────────────────────────────────────

    private static bool IsProductType(JsonElement el)
    {
        if (!el.TryGetProperty("@type", out var t)) return false;

        static bool Match(string? s) =>
            s is not null && (
                s.Equals("Product", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith("/Product", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(":Product", StringComparison.OrdinalIgnoreCase));

        if (t.ValueKind == JsonValueKind.String)
            return Match(t.GetString());
        if (t.ValueKind == JsonValueKind.Array)
            return t.EnumerateArray().Any(x =>
                x.ValueKind == JsonValueKind.String && Match(x.GetString()));
        return false;
    }

    private static string? GetStr(JsonElement el, params string[] keys)
    {
        foreach (var k in keys)
            if (el.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        return null;
    }

    // ── Regex'ler ────────────────────────────────────────────────────────────

    [GeneratedRegex(
        @"<script[^>]+type=[""']application/ld\+json[""'][^>]*>\s*(?<json>[\s\S]*?)\s*</script>",
        RegexOptions.IgnoreCase)]
    private static partial Regex LdJsonRegex();
}
