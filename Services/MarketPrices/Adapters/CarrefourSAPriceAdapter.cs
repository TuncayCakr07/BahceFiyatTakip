using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices.Normalization;
using BahceFiyatTakip.Services.MarketPrices.Validation;

namespace BahceFiyatTakip.Services.MarketPrices.Adapters;

/// <summary>
/// CarrefourSA fiyat adapter'ı.
/// Search: SAP Commerce Cloud REST API → products[].price.value (TRY cinsinden).
/// Direct: JSON-LD (schema.org Product) → HttpClient ile HTML, Playwright gerektirmez.
/// Birim normalizasyonu: UnitNormalizer — Doğrulama: ValidationGate.
/// </summary>
public sealed partial class CarrefourSAPriceAdapter(
    HttpClient httpClient,
    ILogger<CarrefourSAPriceAdapter> logger)
{
    private const string AdapterProvider = "CarrefourSAAdapter";

    // SAP Commerce Cloud (Hybris) arama API'si
    private const string SapApiSearch =
        "https://www.carrefoursa.com/rest/v2/carrefoursa/products/search" +
        "?query={0}&currentPage=0&pageSize=20&sort=relevance&lang=tr&curr=TRY";

    // ── Search: SAP Commerce REST API ─────────────────────────────────────────

    public async Task<MarketPriceResult?> TryFetchSearchAsync(
        Market  market,
        string  query,
        string  productName,
        string  varietyName,
        int?    varietyId,
        string  productUnit,
        CancellationToken ct = default)
    {
        var apiUrl = string.Format(CultureInfo.InvariantCulture, SapApiSearch,
            WebUtility.UrlEncode(query));
        logger.LogInformation("CarrefourSAAdapter: SAP API arama → {Query}", query);

        var body = await FetchAsync(apiUrl, market.BaseUrl, ct);
        if (body is null) return null;

        var candidates = ParseSapJson(body, market.BaseUrl ?? "https://www.carrefoursa.com");
        if (candidates.Count == 0)
        {
            logger.LogInformation("CarrefourSAAdapter: SAP API'den ürün çıkarılamadı. Query: {Q}", query);
            return null;
        }

        return PickBest(candidates, market, productName, varietyName, varietyId, productUnit);
    }

    // ── Direct: JSON-LD (ürün sayfası HTML) ──────────────────────────────────

    public async Task<MarketPriceResult?> TryFetchDirectAsync(
        Market  market,
        string  directUrl,
        string  productName,
        string  varietyName,
        int?    varietyId,
        string  productUnit,
        CancellationToken ct = default)
    {
        logger.LogInformation("CarrefourSAAdapter: Direkt URL → {Url}", directUrl);

        var body = await FetchAsync(directUrl, market.BaseUrl, ct);
        if (body is null) return null;

        // 1. JSON-LD (öncelikli — ürün sayfası)
        var candidates = ParseJsonLd(body, directUrl);

        // 2. Yedek: SAP Commerce gömülü JSON (arama API yanıtı formatında ise)
        if (candidates.Count == 0)
            candidates = ParseSapJson(body, market.BaseUrl ?? "https://www.carrefoursa.com");

        if (candidates.Count == 0) return null;

        return PickBest(candidates, market, productName, varietyName, varietyId, productUnit);
    }

    // ── SAP Commerce JSON parsing ─────────────────────────────────────────────

    private static List<CarrefourCandidate> ParseSapJson(string json, string baseUrl)
    {
        var results = new List<CarrefourCandidate>();
        var trimmed = json.TrimStart();
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('[')) return results;

        try
        {
            using var doc = JsonDocument.Parse(json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling     = JsonCommentHandling.Skip
                });

            var root = doc.RootElement;
            if (!root.TryGetProperty("products", out var productsEl)) return results;
            if (productsEl.ValueKind != JsonValueKind.Array) return results;

            foreach (var el in productsEl.EnumerateArray())
            {
                var name = GetStr(el, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                // price.value → TRY (Migros'taki gibi kuruş değil, doğrudan TRY)
                decimal price = 0;
                if (el.TryGetProperty("price", out var priceEl)
                    && priceEl.TryGetProperty("value", out var valEl)
                    && valEl.ValueKind == JsonValueKind.Number
                    && valEl.TryGetDecimal(out var v) && v > 0)
                    price = v;
                if (price <= 0) continue;

                // stock.stockLevelStatus
                bool inStock = true;
                if (el.TryGetProperty("stock", out var stockEl))
                {
                    var status = GetStr(stockEl, "stockLevelStatus") ?? "inStock";
                    inStock = !status.Equals("outOfStock", StringComparison.OrdinalIgnoreCase);
                }

                // url (relative → absolute)
                var relUrl = GetStr(el, "url");
                var url = relUrl is not null
                    ? (relUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? relUrl
                        : baseUrl.TrimEnd('/') + relUrl)
                    : null;

                // images[] → PRIMARY veya ilk resim
                string? img = null;
                if (el.TryGetProperty("images", out var imgsEl)
                    && imgsEl.ValueKind == JsonValueKind.Array)
                {
                    JsonElement? chosen = null;
                    foreach (var imgEl in imgsEl.EnumerateArray())
                    {
                        var imgType = GetStr(imgEl, "imageType") ?? "";
                        if (imgType.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase))
                        { chosen = imgEl; break; }
                        chosen ??= imgEl;
                    }
                    if (chosen.HasValue)
                    {
                        var imgUrl = GetStr(chosen.Value, "url");
                        if (imgUrl is not null)
                            img = imgUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? imgUrl
                                : baseUrl.TrimEnd('/') + imgUrl;
                    }
                }

                results.Add(new CarrefourCandidate(Clean(name), price, url, img, inStock));
            }
        }
        catch (JsonException) { }

        return results;
    }

    // ── JSON-LD parsing (ürün sayfaları) ─────────────────────────────────────

    private static List<CarrefourCandidate> ParseJsonLd(string html, string sourceUrl)
    {
        var results = new List<CarrefourCandidate>();

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
                        TryAddFromLdNode(node, sourceUrl, results);
                    continue;
                }

                if (root.TryGetProperty("@graph", out var graph)
                    && graph.ValueKind == JsonValueKind.Array)
                {
                    foreach (var node in graph.EnumerateArray())
                        TryAddFromLdNode(node, sourceUrl, results);
                    continue;
                }

                TryAddFromLdNode(root, sourceUrl, results);
            }
        }

        return results;
    }

    private static void TryAddFromLdNode(
        JsonElement node, string sourceUrl, List<CarrefourCandidate> results)
    {
        if (!IsProductType(node)) return;

        var name = GetStr(node, "name");
        if (string.IsNullOrWhiteSpace(name)) return;

        decimal price   = 0;
        bool?   inStock = null;
        string? offerUrl = null;

        if (node.TryGetProperty("offers", out var offersEl))
        {
            var offer = offersEl.ValueKind == JsonValueKind.Array
                ? offersEl.EnumerateArray().FirstOrDefault()
                : offersEl;

            if (offer.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            {
                price    = ExtractLdPrice(offer);
                inStock  = ExtractLdInStock(offer);
                offerUrl = GetStr(offer, "url");
            }
        }

        if (price <= 0) price = ExtractLdPrice(node);
        if (price <= 0) return;

        results.Add(new CarrefourCandidate(
            Clean(name), price,
            offerUrl ?? sourceUrl,
            ExtractLdImage(node),
            inStock));
    }

    // ── Candidate selection ───────────────────────────────────────────────────

    private MarketPriceResult? PickBest(
        List<CarrefourCandidate> candidates,
        Market market,
        string productName, string varietyName, int? varietyId, string productUnit)
    {
        var validResults = new List<(MarketPriceResult Result, int Confidence, decimal Price)>();

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
                logger.LogDebug("CarrefourSAAdapter: Ret [{Rule}] '{Name}' → {Reason}",
                    vr.FailedRule, c.Name, vr.RejectReason);
                continue;
            }

            validResults.Add((
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

        var best = validResults
            .OrderByDescending(r => r.Confidence)
            .ThenBy(r => r.Price)
            .Select(r => r.Result)
            .FirstOrDefault();

        if (best is not null)
            logger.LogInformation(
                "CarrefourSAAdapter: ✓ '{Name}' → {Price} TL (confidence:{Score})",
                best.MatchedTitle, best.PricePerKg, best.ConfidenceScore);
        else
            logger.LogInformation(
                "CarrefourSAAdapter: Eşleşen ürün bulunamadı. Ürün: '{Product}'", productName);

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
                    "CarrefourSAAdapter: HTTP {Code}. {Url}", (int)resp.StatusCode, url);
                return null;
            }
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("CarrefourSAAdapter: İstek hatası — {Msg}", ex.Message);
            return null;
        }
    }

    // ── JSON-LD yardımcıları ──────────────────────────────────────────────────

    private static decimal ExtractLdPrice(JsonElement el)
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
        return 0;
    }

    private static bool? ExtractLdInStock(JsonElement offer)
    {
        if (!offer.TryGetProperty("availability", out var av)
            || av.ValueKind != JsonValueKind.String) return null;
        var s = av.GetString() ?? "";
        if (s.Contains("InStock",             StringComparison.OrdinalIgnoreCase) ||
            s.Contains("LimitedAvailability", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Contains("OutOfStock", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("SoldOut",    StringComparison.OrdinalIgnoreCase)) return false;
        return null;
    }

    private static string? ExtractLdImage(JsonElement el)
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

    // ── Genel yardımcılar ─────────────────────────────────────────────────────

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
        req.Headers.Accept.ParseAdd("application/json, text/html;q=0.9, */*;q=0.7");
        req.Headers.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en-US;q=0.8");
        if (!string.IsNullOrEmpty(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            req.Headers.Referrer = uri;
    }

    [GeneratedRegex(
        @"<script[^>]+type=[""']application/ld\+json[""'][^>]*>\s*(?<json>[\s\S]*?)\s*</script>",
        RegexOptions.IgnoreCase)]
    private static partial Regex LdJsonRegex();

    private sealed record CarrefourCandidate(
        string Name, decimal Price, string? Url, string? ImageUrl, bool? InStock);
}
