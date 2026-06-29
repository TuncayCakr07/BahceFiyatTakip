using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices.Adapters;

/// <summary>
/// Macrocenter ürün sayfasındaki JSON-LD (schema.org Product) verisini parse eder.
/// Yalnızca DirectUrl ile çalışır; SearchUrlTemplate kullanmaz.
/// </summary>
public partial class MacrocenterPriceAdapter(
    PlaywrightPageFetcher pageFetcher,
    ILogger<MacrocenterPriceAdapter> logger)
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");
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

        // ── Doğrulama kapıları ───────────────────────────────────────────────

        // Kapı 1: Fiyat zorunlu
        if (price <= 0)
        {
            logger.LogDebug("MacrocenterAdapter: Fiyat yok → '{Name}'", name);
            return null;
        }

        // Kapı 2: Stok yok → kayıt üretme
        if (inStock == false)
        {
            logger.LogInformation("MacrocenterAdapter: Stok yok → '{Name}'", name);
            return null;
        }

        // Kapı 3: Birim normalizasyonu
        var (normalizedPrice, unitLabel) = NormalizePrice(price, name, productUnit);

        // Kapı 4: Ürün adı eşleşmesi + güven skoru
        int confidence = CalculateConfidence(name, productName, varietyName, unitLabel, inStock);

        // Kapı 5: Düşük güven → kayıt üretme
        if (confidence < 40)
        {
            logger.LogInformation(
                "MacrocenterAdapter: Düşük güven ({Score}) → '{Name}' / beklenen '{Prod}'",
                confidence, name, productName);
            return null;
        }

        var sourceUrl = !string.IsNullOrWhiteSpace(offerUrl) ? offerUrl : directUrl;

        logger.LogInformation(
            "MacrocenterAdapter: ✓ '{Name}' → {Price} TL/{Unit} (confidence:{Score}, inStock:{Stock})",
            name, normalizedPrice, unitLabel, confidence,
            inStock.HasValue ? inStock.Value.ToString() : "?");

        return new MarketPriceResult(
            market.Id,
            market.Name,
            normalizedPrice,
            sourceUrl,
            AdapterProvider,
            IsLive: true,
            ProductVarietyId: varietyId,
            MatchedTitle: name,
            ImageUrl: imageUrl,
            ConfidenceScore: confidence,
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

    // ── Birim normalizasyonu ─────────────────────────────────────────────────

    private static (decimal NormalizedPrice, string UnitLabel) NormalizePrice(
        decimal rawPrice, string productName, string productUnit)
    {
        // "1 kg", "1,5 kg", "1.5kg", "0.5 KG"
        var kgMatch = KgAmountRegex().Match(productName);
        if (kgMatch.Success)
        {
            var amtStr = kgMatch.Groups["a"].Value.Replace(",", ".");
            if (decimal.TryParse(amtStr, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var kg) && kg > 0)
                return (decimal.Round(rawPrice / kg, 2), "kg");
        }

        // "500 g", "500g", "250 gr", "250 gram"
        var gMatch = GramAmountRegex().Match(productName);
        if (gMatch.Success)
        {
            if (int.TryParse(gMatch.Groups["a"].Value, out var grams) && grams > 0)
                return (decimal.Round(rawPrice / (grams / 1000m), 2), "kg");
        }

        // "2 adet", "3 adet" — fiyatı adete böl
        var adetMatch = AdetAmountRegex().Match(productName);
        if (adetMatch.Success)
        {
            if (int.TryParse(adetMatch.Groups["a"].Value, out var adet) && adet > 1)
                return (decimal.Round(rawPrice / adet, 2), "adet");
            return (rawPrice, "adet");
        }

        // Sistemdeki ürün birimini fallback olarak kullan
        var sysUnit = productUnit.ToLower(TrCulture).Trim();
        if (sysUnit == "adet")                            return (rawPrice, "adet");
        if (sysUnit is "kg" or "kilo" or "kilogram")      return (rawPrice, "kg");

        return (rawPrice, "bilinmiyor");
    }

    // ── Güven skoru ───────────────────────────────────────────────────────────

    private static int CalculateConfidence(
        string matchedName, string productName, string varietyName,
        string unitLabel,   bool? inStock)
    {
        var hay  = N(matchedName);
        var prod = N(productName);
        var var_ = N(varietyName);

        int score = 50; // temel: JSON-LD Product bulundu ve fiyat çıkarıldı

        if (ContainsTurkishWord(hay, prod)) score += 20;
        else score -= 30; // ürün adı başlıkta yok → güçlü ceza

        if (!string.IsNullOrEmpty(var_) && var_ != prod && ContainsTurkishWord(hay, var_))
            score += 10;

        score += unitLabel switch
        {
            "kg" or "adet" => 10,
            "bilinmiyor"   => -15,
            _              =>   0,
        };

        score += inStock switch
        {
            true  =>  10,
            null  =>  -5,
            false =>   0,
        };

        return Math.Clamp(score, 0, 100);
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

    private static string N(string s) =>
        s.ToLower(TrCulture)
         .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
         .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");

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

    // ── Regex'ler ────────────────────────────────────────────────────────────

    [GeneratedRegex(
        @"<script[^>]+type=[""']application/ld\+json[""'][^>]*>\s*(?<json>[\s\S]*?)\s*</script>",
        RegexOptions.IgnoreCase)]
    private static partial Regex LdJsonRegex();

    // "1 kg", "1,5 kg", "1.5kg", "2 KG"
    [GeneratedRegex(@"(?<a>\d+(?:[,\.]\d+)?)\s*kg\b", RegexOptions.IgnoreCase)]
    private static partial Regex KgAmountRegex();

    // "500 g", "500g", "250 gr", "250 gram"
    [GeneratedRegex(@"(?<a>\d+)\s*(?:gram|gr|g)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GramAmountRegex();

    // "2 adet", "3adet"
    [GeneratedRegex(@"(?<a>\d+)\s*adet\b", RegexOptions.IgnoreCase)]
    private static partial Regex AdetAmountRegex();
}
