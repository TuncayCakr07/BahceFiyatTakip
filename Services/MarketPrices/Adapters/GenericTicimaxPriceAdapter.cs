using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices.Normalization;
using BahceFiyatTakip.Services.MarketPrices.Validation;

namespace BahceFiyatTakip.Services.MarketPrices.Adapters;

/// <summary>
/// Ticimax altyapısını kullanan marketler için generic adapter.
/// productDetailModel, ProductModel ve gömülü JSON yapılarını parse eder.
/// </summary>
public sealed partial class GenericTicimaxPriceAdapter(
    HttpClient httpClient,
    ILogger<GenericTicimaxPriceAdapter> logger)
{
    private const string AdapterProvider = "GenericTicimax";

    public async Task<MarketPriceResult?> TryFetchDirectAsync(
        Market  market,
        string  directUrl,
        string  productName,
        string  varietyName,
        int?    varietyId,
        string  productUnit,
        CancellationToken ct = default)
    {
        logger.LogInformation("GenericTicimax [{Market}]: Direkt URL → {Url}", market.Name, directUrl);

        var html = await FetchAsync(directUrl, market.BaseUrl, ct);
        if (html is null) return null;

        var candidates = ParseTicimaxData(html, directUrl);
        if (candidates.Count == 0) return null;

        return PickBest(candidates, market, productName, varietyName, varietyId, productUnit);
    }

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

        var candidates = ParseTicimaxData(html, searchUrl);
        if (candidates.Count == 0) return null;

        return PickBest(candidates, market, productName, varietyName, varietyId, productUnit);
    }

    private static List<TicimaxCandidate> ParseTicimaxData(string html, string sourceUrl)
    {
        var results = new List<TicimaxCandidate>();

        // 1. productDetailModel (Classic Ticimax)
        var detailMatch = ProductDetailModelRegex().Match(html);
        if (detailMatch.Success)
        {
            var json = WebUtility.HtmlDecode(detailMatch.Groups["json"].Value).Trim();
            WalkJson(json, sourceUrl, results);
        }

        // 2. ProductModel (Modern Ticimax)
        var modelMatch = ProductModelRegex().Match(html);
        if (modelMatch.Success)
        {
            var json = WebUtility.HtmlDecode(modelMatch.Groups["json"].Value).Trim();
            WalkJson(json, sourceUrl, results);
        }

        // 3. __NEXT_DATA__ (Next.js based Ticimax)
        var nextMatch = NextDataRegex().Match(html);
        if (nextMatch.Success)
        {
            var json = WebUtility.HtmlDecode(nextMatch.Groups["json"].Value).Trim();
            WalkJson(json, sourceUrl, results);
        }

        // 4. JSON-LD
        foreach (Match ld in LdJsonRegex().Matches(html))
        {
            var json = WebUtility.HtmlDecode(ld.Groups["json"].Value).Trim();
            WalkJson(json, sourceUrl, results);
        }

        return results;
    }

    private static void WalkJson(string json, string sourceUrl, List<TicimaxCandidate> results)
    {
        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            WalkElement(doc.RootElement, sourceUrl, results, 0);
        }
        catch (JsonException) { }
    }

    private static void WalkElement(JsonElement el, string sourceUrl, List<TicimaxCandidate> results, int depth)
    {
        if (depth > 10) return;

        if (el.ValueKind == JsonValueKind.Object)
        {
            var c = TryBuildCandidate(el, sourceUrl);
            if (c is not null) results.Add(c);

            foreach (var prop in el.EnumerateObject())
                WalkElement(prop.Value, sourceUrl, results, depth + 1);
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                WalkElement(child, sourceUrl, results, depth + 1);
        }
    }

    private static TicimaxCandidate? TryBuildCandidate(JsonElement el, string sourceUrl)
    {
        // İsim adayları
        var name = GetStr(el, "productName", "name", "title", "displayName", "label");
        if (string.IsNullOrWhiteSpace(name)) return null;

        // Fiyat adayları
        decimal price = 0;
        foreach (var key in new[] { "satisFiyati", "indirimliFiyati", "price", "salePrice", "discountedPrice", "unitPrice", "amount", "value" })
        {
            if (el.TryGetProperty(key, out var pEl))
            {
                price = ExtractDecimal(pEl);
                if (price > 0) break;
            }
        }

        if (price <= 0) return null;

        // Stok
        bool? inStock = GetBool(el, "stokAdedi", "inStock", "available", "isAvailable", "hasStock");
        if (inStock == null && el.TryGetProperty("stokAdedi", out var sEl) && sEl.ValueKind == JsonValueKind.Number)
        {
            inStock = sEl.TryGetInt32(out var count) && count > 0;
        }

        // URL
        var url = GetStr(el, "url", "productUrl", "slug", "link") ?? sourceUrl;
        if (!url.StartsWith("http")) url = $"https://{url}";

        // Resim
        var img = GetStr(el, "imageUrl", "image", "thumbnail");

        return new TicimaxCandidate(name, price, url, img, inStock);
    }

    private static decimal ExtractDecimal(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d) && d > 0) return d;
        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString()?.Replace("TL", "").Replace("₺", "").Replace(" ", "").Replace(",", ".");
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var res) && res > 0) return res;
        }
        return 0;
    }

    private static bool? GetBool(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (el.TryGetProperty(key, out var v))
            {
                if (v.ValueKind == JsonValueKind.True) return true;
                if (v.ValueKind == JsonValueKind.False) return false;
                if (v.ValueKind == JsonValueKind.String && bool.TryParse(v.GetString(), out var b)) return b;
            }
        }
        return null;
    }

    private static string? GetStr(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        }
        return null;
    }

    private MarketPriceResult? PickBest(
        List<TicimaxCandidate> candidates,
        Market market,
        string productName, string varietyName, int? varietyId, string productUnit)
    {
        var valid = new List<(MarketPriceResult Result, int Confidence, decimal Price)>();

        foreach (var c in candidates)
        {
            var norm = UnitNormalizer.Normalize(c.Name, c.Price, productUnit);
            var unitLabel = norm.IsReliable ? norm.NormalizedUnit : "bilinmiyor";

            var vr = ValidationGate.Validate(new CandidateInput(
                c.Name, norm.NormalizedPrice, unitLabel,
                InStock: c.InStock,
                ExpectedProduct: productName,
                ExpectedVariety: varietyName));

            if (!vr.IsValid) continue;

            valid.Add((
                new MarketPriceResult(
                    market.Id, market.Name,
                    norm.NormalizedPrice,
                    c.Url ?? "",
                    AdapterProvider,
                    IsLive: true,
                    ProductVarietyId: varietyId,
                    MatchedTitle: c.Name,
                    ImageUrl: c.ImageUrl,
                    ConfidenceScore: vr.ConfidenceScore,
                    InStock: c.InStock),
                vr.ConfidenceScore,
                norm.NormalizedPrice));
        }

        var best = valid
            .OrderByDescending(r => r.Confidence)
            .ThenBy(r => r.Price)
            .Select(r => r.Result)
            .FirstOrDefault();

        return best;
    }

    private async Task<string?> FetchAsync(string url, string? baseUrl, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            using var resp = await httpClient.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch { return null; }
    }

    [GeneratedRegex(@"productDetailModel\s*=\s*(\{[\s\S]*?\});", RegexOptions.IgnoreCase)]
    private static partial Regex ProductDetailModelRegex();

    [GeneratedRegex(@"productModel\s*=\s*(\{[\s\S]*?\});", RegexOptions.IgnoreCase)]
    private static partial Regex ProductModelRegex();

    [GeneratedRegex(@"<script[^>]+id=[""']__NEXT_DATA__[""'][^>]*>(?<json>[\s\S]*?)</script>", RegexOptions.IgnoreCase)]
    private static partial Regex NextDataRegex();

    [GeneratedRegex(@"<script[^>]+type=[""']application/ld\+json[""'][^>]*>(?<json>[\s\S]*?)</script>", RegexOptions.IgnoreCase)]
    private static partial Regex LdJsonRegex();

    private sealed record TicimaxCandidate(string Name, decimal Price, string? Url, string? ImageUrl, bool? InStock);
}
