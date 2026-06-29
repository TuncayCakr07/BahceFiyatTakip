using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices.Normalization;
using BahceFiyatTakip.Services.MarketPrices.Validation;

namespace BahceFiyatTakip.Services.MarketPrices.Adapters;

/// <summary>
/// Migros REST JSON API'sini kullanarak fiyat çeker.
/// SearchUrlTemplate (arama) ve DirectUrl (ürün sayfası) her ikisini destekler.
/// Birim normalizasyonu: UnitNormalizer — Doğrulama: ValidationGate.
/// </summary>
public class MigrosPriceAdapter(HttpClient httpClient, ILogger<MigrosPriceAdapter> logger)
{
    private const string AdapterProvider = "MigrosAdapter";

    // ── Search: SearchUrlTemplate → Migros REST API ──────────────────────────

    public async Task<MarketPriceResult?> TryFetchSearchAsync(
        Market  market,
        string  query,
        string  productName,
        string  varietyName,
        int?    varietyId,
        string  productUnit,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(market.SearchUrlTemplate))
            return null;

        var searchUrl = BuildUrl(market.SearchUrlTemplate, query);
        logger.LogInformation("MigrosAdapter: Arama → {Query}", query);

        var body = await FetchAsync(searchUrl, market.BaseUrl, ct);
        if (body is null) return null;

        var candidates = ParseMigrosJson(body, searchUrl);
        if (candidates.Count == 0)
        {
            logger.LogInformation("MigrosAdapter: JSON'dan ürün çıkarılamadı. Query: {Q}", query);
            return null;
        }

        return PickBest(candidates, market, productName, varietyName, varietyId, productUnit);
    }

    // ── Direct: DirectUrl → ürün sayfası (JSON API yanıtıysa parse et) ───────

    public async Task<MarketPriceResult?> TryFetchDirectAsync(
        Market  market,
        string  directUrl,
        string  productName,
        string  varietyName,
        int?    varietyId,
        string  productUnit,
        CancellationToken ct = default)
    {
        logger.LogInformation("MigrosAdapter: Direkt URL → {Url}", directUrl);

        var body = await FetchAsync(directUrl, market.BaseUrl, ct);
        if (body is null) return null;

        // Migros ürün sayfaları genellikle HTML döner; sadece JSON varsa parse et
        var trimmed = body.TrimStart();
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
            return null;

        var candidates = ParseMigrosJson(body, directUrl);
        if (candidates.Count == 0) return null;

        return PickBest(candidates, market, productName, varietyName, varietyId, productUnit);
    }

    // ── JSON parsing ─────────────────────────────────────────────────────────

    private static List<MigrosCandidate> ParseMigrosJson(string json, string sourceUrl)
    {
        var results = new List<MigrosCandidate>();
        try
        {
            using var doc = JsonDocument.Parse(json,
                new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });

            if (!doc.RootElement.TryGetProperty("data", out var data)) return results;

            var found = data.TryGetProperty("storeProductInfos", out var infos)
                     || data.TryGetProperty("products",          out infos);
            if (!found) return results;

            foreach (var el in infos.EnumerateArray())
            {
                var name = GetStr(el, "name", "productName");
                if (string.IsNullOrWhiteSpace(name)) continue;

                // shownPrice kuruş cinsinden (29995 → 299.95 TL)
                decimal price = 0;
                foreach (var pKey in new[] { "shownPrice", "regularPrice", "salePrice", "unitPrice" })
                {
                    if (!el.TryGetProperty(pKey, out var pEl)) continue;
                    if (pEl.ValueKind != JsonValueKind.Number) continue;
                    if (!pEl.TryGetDecimal(out var raw) || raw <= 0) continue;
                    price = raw >= 100 ? decimal.Round(raw / 100m, 2) : raw;
                    break;
                }
                if (price <= 0) continue;

                var status  = GetStr(el, "status") ?? "";
                var inStock = !status.Equals("OUT_OF_STOCK", StringComparison.OrdinalIgnoreCase)
                           && !status.Equals("PASSIVE",      StringComparison.OrdinalIgnoreCase);

                var slug = GetStr(el, "prettyName", "slug", "url");
                var url  = slug is not null && !slug.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? $"https://www.migros.com.tr/{slug.TrimStart('/')}"
                    : slug ?? sourceUrl;

                string? img = null;
                if (el.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var imgEl in imgs.EnumerateArray())
                    {
                        if (imgEl.TryGetProperty("urls", out var urls))
                        {
                            img = GetStr(urls, "PRODUCT_LIST", "PRODUCT_DETAIL", "PRODUCT_HD");
                            if (img is not null) break;
                        }
                        img ??= GetStr(imgEl, "url", "imageUrl");
                        if (img is not null) break;
                    }
                }

                results.Add(new MigrosCandidate(Clean(name), price, url, img, inStock));
            }
        }
        catch (JsonException) { }

        return results;
    }

    // ── Candidate selection ───────────────────────────────────────────────────

    private MarketPriceResult? PickBest(
        List<MigrosCandidate> candidates,
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
                logger.LogDebug("MigrosAdapter: Ret [{Rule}] '{Name}' → {Reason}",
                    vr.FailedRule, c.Name, vr.RejectReason);
                continue;
            }

            validResults.Add((
                new MarketPriceResult(
                    market.Id, market.Name,
                    norm.NormalizedPrice,
                    c.Url ?? "",
                    AdapterProvider,
                    IsLive:          true,
                    ProductVarietyId: varietyId,
                    MatchedTitle:    c.Name,
                    ImageUrl:        c.ImageUrl,
                    ConfidenceScore: vr.ConfidenceScore,
                    InStock:         c.InStock),
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
                "MigrosAdapter: ✓ '{Name}' → {Price} TL (confidence:{Score})",
                best.MatchedTitle, best.PricePerKg, best.ConfidenceScore);
        else
            logger.LogInformation(
                "MigrosAdapter: Eşleşen ürün bulunamadı. Ürün: '{Product}'", productName);

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
                logger.LogInformation("MigrosAdapter: HTTP {Code}. {Url}", (int)resp.StatusCode, url);
                return null;
            }
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("MigrosAdapter: İstek hatası — {Msg}", ex.Message);
            return null;
        }
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    // Case-insensitive alan okuma (Migros API tutarsız büyük/küçük harf kullanabilir)
    private static string? GetStr(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
            foreach (var prop in el.EnumerateObject())
            {
                if (!prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
                if (prop.Value.ValueKind == JsonValueKind.String) return prop.Value.GetString();
            }
        return null;
    }

    private static string Clean(string s) =>
        Regex.Replace(WebUtility.HtmlDecode(s), @"\s+", " ").Trim();

    private static string BuildUrl(string template, string query) =>
        string.Format(CultureInfo.InvariantCulture, template, WebUtility.UrlEncode(query));

    private static void SetHeaders(HttpRequestMessage req, string? baseUrl)
    {
        req.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        req.Headers.Accept.ParseAdd("application/json, text/html;q=0.9, */*;q=0.7");
        req.Headers.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en-US;q=0.8");
        if (!string.IsNullOrEmpty(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            req.Headers.Referrer = uri;
    }

    private sealed record MigrosCandidate(
        string Name, decimal Price, string? Url, string? ImageUrl, bool InStock);
}
