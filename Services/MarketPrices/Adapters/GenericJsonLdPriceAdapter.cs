using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices.Normalization;
using BahceFiyatTakip.Services.MarketPrices.Validation;

namespace BahceFiyatTakip.Services.MarketPrices.Adapters;

/// <summary>
/// Dedicated adapter'ı olmayan tüm marketler için catch-all JSON-LD adapter.
/// DirectUrl → schema.org Product JSON-LD parse.
/// Search  → arama sayfasından JSON-LD Product dene; yoksa null döner (fallback zinciri devreye girer).
/// UnitNormalizer + ValidationGate entegrasyonu.
/// </summary>
public sealed partial class GenericJsonLdPriceAdapter(
    HttpClient httpClient,
    ILogger<GenericJsonLdPriceAdapter> logger)
{
    private const string AdapterProvider = "GenericJsonLd";

    // ── DirectUrl: ürün sayfası HTML → JSON-LD ────────────────────────────────

    public async Task<MarketPriceResult?> TryFetchDirectAsync(
        Market  market,
        string  directUrl,
        string  productName,
        string  varietyName,
        int?    varietyId,
        string  productUnit,
        CancellationToken ct = default)
    {
        logger.LogInformation("GenericJsonLd [{Market}]: Direkt URL → {Url}", market.Name, directUrl);

        var html = await FetchAsync(directUrl, market.BaseUrl, ct);
        if (html is null) return null;

        var candidates = ParseJsonLd(html, directUrl);
        if (candidates.Count == 0)
        {
            logger.LogInformation(
                "GenericJsonLd [{Market}]: JSON-LD Product bulunamadı. {Url}", market.Name, directUrl);
            return null;
        }

        return PickBest(candidates, market, productName, varietyName, varietyId, productUnit);
    }

    // ── Search: arama sayfası HTML → JSON-LD dene ────────────────────────────

    public async Task<MarketPriceResult?> TryFetchSearchAsync(
        Market  market,
        string  query,
        string  productName,
        string  varietyName,
        int?    varietyId,
        string  productUnit,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(market.SearchUrlTemplate)) return null;

        var searchUrl = string.Format(CultureInfo.InvariantCulture,
            market.SearchUrlTemplate, WebUtility.UrlEncode(query));

        var html = await FetchAsync(searchUrl, market.BaseUrl, ct);
        if (html is null) return null;

        var candidates = ParseJsonLd(html, searchUrl);
        if (candidates.Count == 0) return null;

        return PickBest(candidates, market, productName, varietyName, varietyId, productUnit);
    }

    // ── JSON-LD parsing ───────────────────────────────────────────────────────

    private static List<JsonLdCandidate> ParseJsonLd(string html, string sourceUrl)
    {
        var results = new List<JsonLdCandidate>();

        foreach (Match block in LdJsonRegex().Matches(html))
        {
            var jsonText = WebUtility.HtmlDecode(block.Groups["json"].Value).Trim();
            if (string.IsNullOrEmpty(jsonText)) continue;

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(jsonText,
                    new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling     = JsonCommentHandling.Skip
                    });
            }
            catch (JsonException) { continue; }

            using (doc)
            {
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var node in root.EnumerateArray())
                        TryAddNode(node, sourceUrl, results);
                    continue;
                }

                if (root.TryGetProperty("@graph", out var graph)
                    && graph.ValueKind == JsonValueKind.Array)
                {
                    foreach (var node in graph.EnumerateArray())
                        TryAddNode(node, sourceUrl, results);
                    continue;
                }

                TryAddNode(root, sourceUrl, results);
            }
        }

        return results;
    }

    private static void TryAddNode(
        JsonElement el, string sourceUrl, List<JsonLdCandidate> results)
    {
        if (!IsProductType(el)) return;

        var name = GetStr(el, "name");
        if (string.IsNullOrWhiteSpace(name)) return;

        decimal price    = 0;
        bool?   inStock  = null;
        string? offerUrl = null;

        if (el.TryGetProperty("offers", out var offersEl))
        {
            var offer = offersEl.ValueKind == JsonValueKind.Array
                ? offersEl.EnumerateArray().FirstOrDefault()
                : offersEl;

            if (offer.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            {
                price    = ExtractPrice(offer);
                inStock  = ExtractInStock(offer);
                offerUrl = GetStr(offer, "url");
            }
        }

        if (price <= 0) price = ExtractPrice(el);
        if (price <= 0) return;

        results.Add(new JsonLdCandidate(
            Clean(name), price,
            offerUrl ?? sourceUrl,
            ExtractImage(el),
            inStock));
    }

    // ── Candidate selection ───────────────────────────────────────────────────

    private MarketPriceResult? PickBest(
        List<JsonLdCandidate> candidates,
        Market market,
        string productName, string varietyName, int? varietyId, string productUnit)
    {
        var valid = new List<(MarketPriceResult Result, int Confidence, decimal Price)>();

        foreach (var c in candidates)
        {
            var norm      = UnitNormalizer.Normalize(c.Name, c.Price, productUnit);
            var unitLabel = norm.IsReliable ? norm.NormalizedUnit : "bilinmiyor";

            var vr = ValidationGate.Validate(new CandidateInput(
                c.Name, norm.NormalizedPrice, unitLabel,
                InStock:         c.InStock,
                ExpectedProduct: productName,
                ExpectedVariety: varietyName));

            if (!vr.IsValid)
            {
                logger.LogDebug("GenericJsonLd [{Market}]: Ret [{Rule}] '{Name}'",
                    market.Name, vr.FailedRule, c.Name);
                continue;
            }

            valid.Add((
                new MarketPriceResult(
                    market.Id, market.Name,
                    norm.NormalizedPrice,
                    c.Url ?? "",
                    AdapterProvider,
                    IsLive:           true,
                    ProductVarietyId: varietyId,
                    MatchedTitle:     c.Name,
                    ImageUrl:         c.ImageUrl,
                    ConfidenceScore:  vr.ConfidenceScore,
                    InStock:          c.InStock),
                vr.ConfidenceScore,
                norm.NormalizedPrice));
        }

        var best = valid
            .OrderByDescending(r => r.Confidence)
            .ThenBy(r => r.Price)
            .Select(r => r.Result)
            .FirstOrDefault();

        if (best is not null)
            logger.LogInformation(
                "GenericJsonLd [{Market}]: ✓ '{Name}' → {Price} TL (conf:{Score})",
                market.Name, best.MatchedTitle, best.PricePerKg, best.ConfidenceScore);
        else
            logger.LogInformation(
                "GenericJsonLd [{Market}]: Eşleşen ürün yok. '{Product}'",
                market.Name, productName);

        return best;
    }

    // ── HTTP ──────────────────────────────────────────────────────────────────

    private async Task<string?> FetchAsync(string url, string? baseUrl, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            SetHeaders(req, baseUrl);
            using var resp = await httpClient.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "GenericJsonLd: HTTP {Code}. {Url}", (int)resp.StatusCode, url);
                return null;
            }
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("GenericJsonLd: İstek hatası — {Msg}", ex.Message);
            return null;
        }
    }

    // ── JSON-LD yardımcıları ──────────────────────────────────────────────────

    private static decimal ExtractPrice(JsonElement el)
    {
        foreach (var key in new[] { "price", "lowPrice" })
        {
            if (!el.TryGetProperty(key, out var pEl)) continue;
            if (pEl.ValueKind == JsonValueKind.Number && pEl.TryGetDecimal(out var n) && n > 0)
                return n;
            if (pEl.ValueKind == JsonValueKind.String)
            {
                var s = (pEl.GetString() ?? "")
                    .Replace("TL", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("₺", "")
                    .Replace(" ", "")
                    .Replace(",", ".");
                if (decimal.TryParse(s, NumberStyles.Number,
                        CultureInfo.InvariantCulture, out var d) && d > 0)
                    return d;
            }
        }

        // priceSpecification fallback
        if (el.TryGetProperty("priceSpecification", out var spec))
        {
            var node = spec.ValueKind == JsonValueKind.Array
                ? spec.EnumerateArray().FirstOrDefault()
                : spec;
            if (node.ValueKind is not JsonValueKind.Undefined
                && node.TryGetProperty("price", out var pp))
            {
                if (pp.ValueKind == JsonValueKind.Number
                    && pp.TryGetDecimal(out var pn) && pn > 0) return pn;
                if (pp.ValueKind == JsonValueKind.String)
                {
                    var s = (pp.GetString() ?? "")
                        .Replace(",", ".").Replace(" ", "").Replace("₺", "");
                    if (decimal.TryParse(s, NumberStyles.Number,
                            CultureInfo.InvariantCulture, out var pd) && pd > 0) return pd;
                }
            }
        }

        return 0;
    }

    private static bool? ExtractInStock(JsonElement offer)
    {
        if (!offer.TryGetProperty("availability", out var av)
            || av.ValueKind != JsonValueKind.String) return null;
        var s = av.GetString() ?? "";
        if (s.Contains("InStock",             StringComparison.OrdinalIgnoreCase) ||
            s.Contains("LimitedAvailability", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("PreOrder",            StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Contains("OutOfStock", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("SoldOut",    StringComparison.OrdinalIgnoreCase)) return false;
        return null;
    }

    private static string? ExtractImage(JsonElement el)
    {
        if (!el.TryGetProperty("image", out var img)) return null;
        if (img.ValueKind == JsonValueKind.String) return img.GetString();
        if (img.ValueKind == JsonValueKind.Array)
        {
            var first = img.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.String) return first.GetString();
            if (first.ValueKind == JsonValueKind.Object) return GetStr(first, "url", "contentUrl");
        }
        return img.ValueKind == JsonValueKind.Object ? GetStr(img, "url", "contentUrl") : null;
    }

    private static bool IsProductType(JsonElement el)
    {
        if (!el.TryGetProperty("@type", out var t)) return false;
        static bool Match(string? s) =>
            s is not null && (
                s.Equals("Product", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith("/Product", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(":Product", StringComparison.OrdinalIgnoreCase));
        if (t.ValueKind == JsonValueKind.String) return Match(t.GetString());
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

    private static string Clean(string s) =>
        Regex.Replace(WebUtility.HtmlDecode(s), @"\s+", " ").Trim();

    private static void SetHeaders(HttpRequestMessage req, string? baseUrl)
    {
        req.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        req.Headers.Accept.ParseAdd("text/html, application/json, */*;q=0.7");
        req.Headers.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en-US;q=0.8");
        if (!string.IsNullOrEmpty(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            req.Headers.Referrer = uri;
    }

    [GeneratedRegex(
        @"<script[^>]+type=[""']application/ld\+json[""'][^>]*>\s*(?<json>[\s\S]*?)\s*</script>",
        RegexOptions.IgnoreCase)]
    private static partial Regex LdJsonRegex();

    private sealed record JsonLdCandidate(
        string Name, decimal Price, string? Url, string? ImageUrl, bool? InStock);
}
