using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

/// <summary>
/// Marketlerin JSON API'lerine direkt istek atar.
/// HTML scraping yok, tarayıcı yok.
/// </summary>
public partial class LiveMarketPriceProvider(
    HttpClient httpClient,
    ILogger<LiveMarketPriceProvider> logger,
    PlaywrightPageFetcher pageFetcher) : IMarketPriceProvider
{
    public string ProviderName => "MarketJson";

    public async Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(
        Product product,
        IReadOnlyList<Market> markets,
        CancellationToken cancellationToken = default)
    {
        var targets = BuildTargets(product);

        // Arama URL'i olan aktif marketler
        var searchMarketIds = markets
            .Where(m => m.IsActive && !string.IsNullOrWhiteSpace(m.SearchUrlTemplate))
            .Select(m => m.Id)
            .ToHashSet();

        // Direkt URL'i olan ama arama listesinde olmayan marketler (aktif değil olabilir)
        var directLinkMarketIds = product.Varieties
            .SelectMany(v => v.DirectLinks.Where(l => l.IsActive))
            .Select(l => l.MarketId)
            .Distinct()
            .Where(id => !searchMarketIds.Contains(id))
            .ToHashSet();

        var allMarkets = markets
            .Where(m => searchMarketIds.Contains(m.Id) || directLinkMarketIds.Contains(m.Id))
            .ToList();

        var tasks = allMarkets
            .Select(m => FetchMarketAsync(product, m, targets, cancellationToken));

        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r is not null)
            .Select(r => r!)
            .OrderBy(r => r.MarketName)
            .ToList();
    }

    // ── PER-MARKET FETCH ─────────────────────────────────────────────────────

    private async Task<MarketPriceResult?> FetchMarketAsync(
        Product product,
        Market market,
        IReadOnlyList<SearchTarget> targets,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(12));

        try
        {
            // ── ADIM 0: Direkt URL'ler (Excel'den elle girilmiş kesin ürün sayfaları) ──
            var directLinks = product.Varieties
                .SelectMany(v => v.DirectLinks.Where(l => l.MarketId == market.Id && l.IsActive))
                .ToList();

            foreach (var dl in directLinks)
            {
                var dTarget = targets.FirstOrDefault(t => t.VarietyId == dl.ProductVarietyId)
                              ?? targets.FirstOrDefault();
                if (dTarget is null) continue;

                logger.LogInformation("{Market}: Direkt URL deneniyor: {Url}", market.Name, dl.DirectUrl);
                var (dResult, _) = await FetchOneAsync(product, market, dTarget, dl.DirectUrl, cts.Token);
                if (dResult is not null)
                    // Direkt URL el ile doğrulanmış → confidence en az 75
                    return dResult with { ConfidenceScore = Math.Max(dResult.ConfidenceScore, 75) };

                // HttpClient başarısız → NetworkIdle bekleyen Playwright ile dene (JS-rendered sayfalar)
                if (!pageFetcher.IsAvailable) continue;
                logger.LogInformation("{Market}: Direkt URL Playwright (NetworkIdle) ile deneniyor: {Url}", market.Name, dl.DirectUrl);
                var pwHtml = await pageFetcher.FetchProductPageAsync(dl.DirectUrl, cts.Token);
                if (pwHtml is null) continue;
                var pwDirItems = ExtractItems(pwHtml, market, dl.DirectUrl);
                var pwDirBest  = pwDirItems.Count > 0 ? PickBest(pwDirItems, product, dTarget) : null;
                if (pwDirBest is not null)
                {
                    logger.LogInformation("{Market}: Playwright direkt '{Title}' → {Price} TL", market.Name, pwDirBest.Title, pwDirBest.Price);
                    return new MarketPriceResult(
                        market.Id, market.Name,
                        pwDirBest.Price, pwDirBest.Url ?? dl.DirectUrl, ProviderName,
                        IsLive: true, ProductVarietyId: dTarget.VarietyId,
                        MatchedTitle: pwDirBest.Title, ImageUrl: pwDirBest.ImageUrl,
                        ConfidenceScore: Math.Max(pwDirBest.Score, 75));
                }
            }

            // Direkt URL bulunamadı; arama URL'i yoksa çık
            if (string.IsNullOrWhiteSpace(market.SearchUrlTemplate))
                return null;

            // ── ADIM 1: HttpClient ile arama ──
            bool marketSupportsJson = true;
            foreach (var target in targets.Take(2))
            {
                if (!marketSupportsJson) break;
                var url = BuildUrl(market.SearchUrlTemplate!, target.Query);
                var (result, hasJson) = await FetchOneAsync(product, market, target, url, cts.Token);
                marketSupportsJson = hasJson;
                if (result is not null)
                    return result;
            }

            // HttpClient başarısız → Playwright ile dene (JS-render + WooCommerce ürün sayfaları)
            if (!pageFetcher.IsAvailable)
                return null;

            foreach (var target in targets.Take(2))
            {
                var url = BuildUrl(market.SearchUrlTemplate!, target.Query);
                logger.LogInformation("{Market}: Playwright ile deneniyor. Query: {Q}", market.Name, target.Query);

                // Arama sayfasını yükle + WooCommerce ürün linklerini tek seferde al
                var (rendered, productLinks) = await pageFetcher.FetchWithLinksAsync(
                    url, "a.woocommerce-loop-product__link", cts.Token);

                // Önce arama sonuçları sayfasından JSON çekmeyi dene
                if (rendered is not null)
                {
                    var pwItems = ExtractItems(rendered, market, url);
                    var pwBest = pwItems.Count > 0 ? PickBest(pwItems, product, target) : null;
                    if (pwBest is not null)
                    {
                        logger.LogInformation("{Market}: Playwright '{Title}' → {Price} TL (score:{Score})", market.Name, pwBest.Title, pwBest.Price, pwBest.Score);
                        return new MarketPriceResult(
                            market.Id, market.Name,
                            pwBest.Price, pwBest.Url ?? url, ProviderName,
                            IsLive: true, ProductVarietyId: target.VarietyId,
                            MatchedTitle: pwBest.Title, ImageUrl: pwBest.ImageUrl, ConfidenceScore: pwBest.Score);
                    }
                }

                // JSON bulunamadı → WooCommerce ürün sayfalarını ziyaret et (JSON-LD var)
                if (productLinks.Count > 0)
                {
                    logger.LogInformation("{Market}: {N} WooCommerce ürün linki, sayfalar ziyaret ediliyor. Query: {Q}", market.Name, productLinks.Count, target.Query);
                    foreach (var productUrl in productLinks.Take(3))
                    {
                        var (result, _) = await FetchOneAsync(product, market, target, productUrl, cts.Token);
                        if (result is not null)
                            return result;
                    }
                }
            }

            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("{Market} 12 saniye limitini aştı.", market.Name);
            return null;
        }
    }

    // Returns (result, hasJson): hasJson=false means the market returned non-JSON, skip further queries
    private async Task<(MarketPriceResult? Result, bool HasJson)> FetchOneAsync(
        Product product,
        Market market,
        SearchTarget target,
        string url,
        CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            SetHeaders(req, market.BaseUrl);

            using var resp = await httpClient.SendAsync(req, ct);

            if (!resp.IsSuccessStatusCode)
            {
                logger.LogInformation("{Market}: HTTP {Code}. Query: {Q}", market.Name, (int)resp.StatusCode, target.Query);
                return (null, false);
            }

            var body = await resp.Content.ReadAsStringAsync(ct);

            // Detect JSON capability: only pure-JSON APIs support multi-query retry
            var trimmed = body.TrimStart();
            bool isPureJson = trimmed.StartsWith('{') || trimmed.StartsWith('[');
            bool hasEmbeddedJson = !isPureJson && (NextDataRegex().IsMatch(body) || LdJsonRegex().IsMatch(body));

            if (!isPureJson && !hasEmbeddedJson)
            {
                logger.LogInformation("{Market}: HTML yanıt (JSON yok), atlanıyor. Query: {Q}", market.Name, target.Query);
                return (null, false);
            }

            // isPureJson = supports retry; hasEmbeddedJson = one-shot only
            bool hasJson = isPureJson;

            var items = ExtractItems(body, market, url);

            if (items.Count == 0)
            {
                logger.LogInformation("{Market}: JSON'dan ürün çıkarılamadı. Query: {Q}", market.Name, target.Query);
                return (null, hasJson);
            }

            var best = PickBest(items, product, target);
            if (best is null)
            {
                logger.LogInformation("{Market}: eşleşen ürün bulunamadı. Query: {Q}", market.Name, target.Query);
                return (null, hasJson);
            }

            logger.LogInformation("{Market}: '{Title}' → {Price} TL (score:{Score})", market.Name, best.Title, best.Price, best.Score);

            return (new MarketPriceResult(
                market.Id, market.Name,
                best.Price,
                best.Url ?? url,
                ProviderName,
                IsLive: true,
                ProductVarietyId: target.VarietyId,
                MatchedTitle: best.Title,
                ImageUrl: best.ImageUrl,
                ConfidenceScore: best.Score), hasJson);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException or JsonException)
        {
            logger.LogWarning(ex, "{Market}: istek hatası. Query: {Q}", market.Name, target.Query);
            return (null, false);
        }
    }

    // ── JSON EXTRACTION ──────────────────────────────────────────────────────

    private static List<Candidate> ExtractItems(string body, Market market, string sourceUrl)
    {
        var items = new List<Candidate>();
        var trimmed = body.TrimStart();

        // Market-specific fast paths
        if (market.Name.Equals("Migros", StringComparison.OrdinalIgnoreCase)
            && (trimmed.StartsWith('{') || trimmed.StartsWith('[')))
        {
            ExtractMigros(body, items, market.BaseUrl, sourceUrl);
            return items;
        }

        // 1. Yanıt doğrudan JSON ise
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            ParseJson(body, items, market.BaseUrl, sourceUrl, dividePrice: false);
            if (items.Count > 0) return items;
        }

        // 2. Next.js __NEXT_DATA__ (SSR gömülü JSON)
        var m = NextDataRegex().Match(body);
        if (m.Success)
        {
            ParseJson(WebUtility.HtmlDecode(m.Groups["json"].Value), items, market.BaseUrl, sourceUrl, dividePrice: false);
            if (items.Count > 0) return items;
        }

        // 3. JSON-LD (schema.org Product)
        foreach (Match ld in LdJsonRegex().Matches(body))
        {
            ParseJson(WebUtility.HtmlDecode(ld.Groups["json"].Value), items, market.BaseUrl, sourceUrl, dividePrice: false);
        }

        return items;
    }

    // Migros: data.storeProductInfos[].{name, shownPrice (kuruş), images[0].urls.PRODUCT_LIST, prettyName, status}
    private static void ExtractMigros(string json, List<Candidate> results, string? baseUrl, string sourceUrl)
    {
        try
        {
            using var doc = JsonDocument.Parse(json,
                new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });

            if (!doc.RootElement.TryGetProperty("data", out var data)) return;

            JsonElement infos;
            var found = data.TryGetProperty("storeProductInfos", out infos)
                     || data.TryGetProperty("products",          out infos);
            if (!found) return;

            foreach (var el in infos.EnumerateArray())
            {
                var name = GetStr(el, "name", "productName");
                if (string.IsNullOrWhiteSpace(name)) continue;

                // shownPrice is in kuruş (e.g. 29995 = 299.95 TL); read raw int before NormalizePrice divides
                decimal price = 0;
                foreach (var pKey in new[] { "shownPrice", "regularPrice", "salePrice", "unitPrice" })
                {
                    if (el.TryGetProperty(pKey, out var pEl) && pEl.ValueKind == JsonValueKind.Number
                        && pEl.TryGetDecimal(out var raw) && raw > 0)
                    {
                        price = raw >= 100 ? decimal.Round(raw / 100m, 2) : raw;
                        break;
                    }
                }
                if (price <= 0) continue;

                var status  = GetStr(el, "status") ?? "";
                var inStock = !status.Equals("OUT_OF_STOCK", StringComparison.OrdinalIgnoreCase)
                           && !status.Equals("PASSIVE", StringComparison.OrdinalIgnoreCase);

                var slug  = GetStr(el, "prettyName", "slug", "url");
                var url   = slug is not null && !slug.StartsWith("http")
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

                results.Add(new Candidate(Clean(name), price, url, img, inStock));
            }
        }
        catch (JsonException) { }
    }

    private static void ParseJson(string json, List<Candidate> results, string? baseUrl, string sourceUrl, bool dividePrice)
    {
        try
        {
            using var doc = JsonDocument.Parse(json,
                new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            Walk(doc.RootElement, results, baseUrl, sourceUrl, dividePrice, depth: 0);
        }
        catch (JsonException) { }
    }

    private static void Walk(JsonElement el, List<Candidate> results, string? baseUrl, string sourceUrl, bool dividePrice, int depth)
    {
        if (depth > 10) return;

        if (el.ValueKind == JsonValueKind.Object)
        {
            var c = TryBuildCandidate(el, baseUrl, sourceUrl, dividePrice);
            if (c is not null)
                results.Add(c);

            foreach (var prop in el.EnumerateObject())
                Walk(prop.Value, results, baseUrl, sourceUrl, dividePrice, depth + 1);
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray())
                Walk(child, results, baseUrl, sourceUrl, dividePrice, depth + 1);
        }
    }

    private static Candidate? TryBuildCandidate(JsonElement el, string? baseUrl, string sourceUrl, bool dividePrice)
    {
        var name = GetStr(el, "name", "title", "productName", "displayName", "shortName", "seoName",
                              "urunAdi", "productTitle", "itemName");

        var rawPrice = GetPrice(el, "shownPrice", "salePrice", "discountedPrice", "unitPrice",
                                    "price", "currentPrice", "listPrice", "normalPrice",
                                    "amount", "value", "finalPrice");

        // WooCommerce / JSON-LD schema.org: fiyat "offers" alt objesinin içinde olabilir
        if ((rawPrice is null or <= 0) && el.TryGetProperty("offers", out var offersEl))
        {
            var offer = offersEl.ValueKind == JsonValueKind.Array
                ? offersEl.EnumerateArray().FirstOrDefault()
                : offersEl;
            if (offer.ValueKind != JsonValueKind.Undefined)
                rawPrice = GetPrice(offer, "price", "salePrice", "lowPrice", "highPrice");
        }

        if (string.IsNullOrWhiteSpace(name) || rawPrice is null or <= 0)
            return null;

        var price = dividePrice && rawPrice >= 100 ? decimal.Round(rawPrice.Value / 100m, 2) : decimal.Round(rawPrice.Value, 2);
        if (price <= 0 || price > 9999) return null;

        var url = Resolve(GetStr(el, "url", "productUrl", "link", "seoUrl", "slug", "detailPageUrl"), baseUrl) ?? sourceUrl;
        var img = Resolve(GetStr(el, "imageUrl", "image", "pictureUrl", "picture", "thumbnail",
                                      "squareImage", "img", "imageLink", "mainImage"), baseUrl);

        var stockBool = GetBool(el, "inStock", "isInStock", "available", "isAvailable", "hasStock",
                                     "availableForSale", "isActive");
        var stockStr  = GetStr(el, "stockStatus", "availability", "stockText", "availabilityStatus");
        var inStock   = stockBool ?? !IsOutOfStock(stockStr);

        return new Candidate(Clean(name), price, url, img, inStock);
    }

    // ── BEST CANDIDATE SELECTION ─────────────────────────────────────────────

    private static Candidate? PickBest(List<Candidate> candidates, Product product, SearchTarget target)
    {
        var prodNorm    = N(product.Name);
        var queryNorm   = N(target.Query);
        var varietyNorm = N(target.VarietyName);
        var minPrice    = IsGrocery(product) ? 10m : 5m;

        return candidates
            .Where(c => c.InStock)
            .Where(c => c.Price >= minPrice && c.Price <= 5000)
            .Where(c => !IsJunk(c.Title))
            .Where(c => IsRelevant(c.Title, prodNorm, queryNorm))
            .Select(c =>
            {
                c.Score = CalcScore(c.Title, prodNorm, queryNorm, varietyNorm);
                return c;
            })
            .Where(c => c.Score >= 40)
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Price)
            .FirstOrDefault();
    }

    private static int CalcScore(string title, string prodNorm, string queryNorm, string varietyNorm)
    {
        var hay = N(title);
        var s = 0;
        if (ContainsWord(hay, prodNorm))                                              s += 40;
        if (prodNorm != varietyNorm && ContainsWord(hay, varietyNorm))               s += 25;
        if (prodNorm != queryNorm   && ContainsPhrase(hay, queryNorm))               s += 20;
        if (Any(hay, "kg", "gr", "adet", "taze", "organik", "meyve", "sebze"))      s += 10;
        if (Any(hay, "stok yok", "tukendi", "satis disi"))                           s -= 30;
        return Math.Clamp(s, 0, 100);
    }

    private static bool IsRelevant(string title, string prodNorm, string queryNorm)
    {
        var n = N(title);
        return ContainsWord(n, prodNorm) || ContainsPhrase(n, queryNorm);
    }

    // Word-boundary match: "nar" must not be part of "narenciye"
    private static bool ContainsWord(string hay, string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        var idx = hay.IndexOf(word, StringComparison.Ordinal);
        while (idx >= 0)
        {
            bool startOk = idx == 0         || !char.IsLetter(hay[idx - 1]);
            bool endOk   = idx + word.Length == hay.Length || !char.IsLetter(hay[idx + word.Length]);
            if (startOk && endOk) return true;
            idx = hay.IndexOf(word, idx + 1, StringComparison.Ordinal);
        }
        return false;
    }

    // Phrase match (multi-word): "hass avokado" can span as substring
    private static bool ContainsPhrase(string hay, string phrase) =>
        hay.Contains(phrase, StringComparison.Ordinal);

    private static bool IsJunk(string title)
    {
        var n = N(title);
        return Any(n,
            // içecek / meyve suyu / sıvı
            " suyu", "meyve suyu", "nektar", "kola", "gazoz", "soda", "energy",
            "fanta", "sprite", "pepsi", "lipton", "schweppes", "uludag",
            "ice tea", "smoothie", "icecek", "konsantre", "limonata",
            " ml", " lt ", " 1 l", " 2 l", " 3 l",
            // şeker / tatlı / atıştırmalık / konsantre
            "toz seker", "lokum", "recel", "marmelat", "pekmez", "eksisi",
            " tatlisi", "cikolata", "biskuvi", "gofret", "kek ", "kuru pasta", "dondurma",
            "sekerleme", "aromal", "jole", "puding", "bonbon", "karamel",
            // kraker / cips / atıştırmalık
            "kraker", "cips", "chili", "tortilla", "patlamis",
            // işlenmiş / paket
            "konserve", "tursu", "sos ", "ketcap", "mayonez", "salca",
            "corba", "hazir ", "kuruyemis", "findik ", "badem ",
            "cay ", "kahve",
            // püresi / özlü / bebek maması
            "puresi", "pureli", "ozlu", "karisik meyve", "hipp", "bebek", "besin",
            "macun", "draje", "vitamin", "takviye", "mama", "kavanoz",
            // kişisel bakım / kozmetik / diğer
            "mendil", "kokulu", "koku", "parfum", "deodorant", "sakiz", "gum ",
            "sensation", "sensations", "cool mint", "fresh mint", "freeze",
            " 10 g", " 15 g", " 20 g", " 25 g", " 27 g", " 30 g", " 35 g", " 40 g",
            // sushi / hazır yemek
            "sushi", " roll", "maki", "nigiri", "temaki", "onigiri",
            "dardenia", "daily",
            // ev & temizlik & kişisel bakım
            "deterjan", "sabun", "sampuan", "krem", "losyon", "jel",
            "wc ", "blok ", "domestos", "cif", "veet", "ariel", "persil",
            "temizleyici", "deodorant", "parfum", "sprey",
            // navigasyon
            "anasayfa", "sepet", "kategori");
    }

    private static bool IsGrocery(Product p) =>
        Any($"{p.Category} {p.Name}", "narenciye", "tropikal", "meyve", "sebze", "diger");

    // ── JSON HELPERS ─────────────────────────────────────────────────────────

    private static string? GetStr(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var prop in el.EnumerateObject())
            {
                if (!prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;

                var v = prop.Value;
                if (v.ValueKind == JsonValueKind.String)
                    return v.GetString();
                if (v.ValueKind == JsonValueKind.Object)
                {
                    var nested = GetStr(v, "text", "label", "name", "value", "tr", "formattedValue");
                    if (!string.IsNullOrWhiteSpace(nested)) return nested;
                }
            }
        }
        return null;
    }

    private static decimal? GetPrice(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var prop in el.EnumerateObject())
            {
                if (!prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
                var p = ParsePrice(prop.Value);
                if (p is > 0) return p;
            }
        }
        return null;
    }

    private static decimal? ParsePrice(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var v))
            return NormalizePrice(v);
        if (el.ValueKind == JsonValueKind.String)
            return ParsePriceStr(el.GetString() ?? "");
        if (el.ValueKind == JsonValueKind.Object)
            return GetPrice(el, "value", "amount", "price", "salePrice", "discountedPrice", "formattedValue");
        return null;
    }

    private static decimal? ParsePriceStr(string s)
    {
        s = s.Replace("TL", "", StringComparison.OrdinalIgnoreCase)
             .Replace("TRY", "", StringComparison.OrdinalIgnoreCase)
             .Replace("₺", "").Replace(" ", "").Replace(".", "").Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v > 0
            ? NormalizePrice(v) : null;
    }

    private static decimal NormalizePrice(decimal p)
        => p > 1500 && p % 1 == 0 ? decimal.Round(p / 100, 2) : decimal.Round(p, 2);

    private static bool? GetBool(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var prop in el.EnumerateObject())
            {
                if (!prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
                if (prop.Value.ValueKind == JsonValueKind.True)  return true;
                if (prop.Value.ValueKind == JsonValueKind.False) return false;
                if (prop.Value.ValueKind == JsonValueKind.String
                    && bool.TryParse(prop.Value.GetString(), out var b)) return b;
            }
        }
        return null;
    }

    private static bool IsOutOfStock(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return Any(N(s), "stokta yok", "tukendi", "satis disi", "out of stock", "unavailable", "outofstock", "discontinued");
    }

    private static string? Resolve(string? url, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        url = WebUtility.HtmlDecode(url.Trim());
        if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;
        if (!string.IsNullOrWhiteSpace(baseUrl)
            && Uri.TryCreate(new Uri(baseUrl), url, out var abs))
            return abs.ToString();
        return null;
    }

    private static string Clean(string s) =>
        Regex.Replace(WebUtility.HtmlDecode(s), @"\s+", " ").Trim();

    private static readonly System.Globalization.CultureInfo TrCulture =
        System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    // Turkish-aware normalization: İ→i, I→ı→i, Turkish diacritics removed
    private static string N(string s) =>
        WebUtility.HtmlDecode(s)
            .ToLower(TrCulture)           // İ→i, I→ı  (Turkish locale)
            .Replace("ı","i").Replace("ğ","g").Replace("ü","u")
            .Replace("ş","s").Replace("ö","o").Replace("ç","c");

    private static bool Any(string s, params string[] needles) =>
        needles.Any(n => s.Contains(N(n)));

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

    private static IReadOnlyList<SearchTarget> BuildTargets(Product product)
    {
        var list = product.Varieties
            .Where(v => v.IsActive)
            .Select(v =>
            {
                var q = v.SearchAliases.OrderBy(a => a.Priority).FirstOrDefault()?.Query
                        ?? $"{v.Name} {product.Name}".ToLowerInvariant();
                return new SearchTarget(v.Id, q, v.Name, product.Name);
            })
            .ToList();

        return list.Count > 0
            ? list
            : [new SearchTarget(null, product.Name, product.Name, product.Name)];
    }

    // ── REGEX ─────────────────────────────────────────────────────────────────

    [GeneratedRegex(
        @"<script[^>]+id=[""']__NEXT_DATA__[""'][^>]*>(?<json>[\s\S]*?)</script>",
        RegexOptions.IgnoreCase)]
    private static partial Regex NextDataRegex();

    [GeneratedRegex(
        @"<script[^>]+type=[""']application/ld\+json[""'][^>]*>(?<json>[\s\S]*?)</script>",
        RegexOptions.IgnoreCase)]
    private static partial Regex LdJsonRegex();

    // ── TYPES ─────────────────────────────────────────────────────────────────

    private sealed class Candidate(string title, decimal price, string url, string? imageUrl, bool inStock)
    {
        public string   Title    { get; } = title;
        public decimal  Price    { get; } = price;
        public string   Url      { get; } = url;
        public string?  ImageUrl { get; } = imageUrl;
        public bool     InStock  { get; } = inStock;
        public int      Score    { get; set; }
    }

    private sealed record SearchTarget(int? VarietyId, string Query, string VarietyName, string ProductName);
}
