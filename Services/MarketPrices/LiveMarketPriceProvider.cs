using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;
using BahceFiyatTakip.Services.MarketPrices.Extractors;

namespace BahceFiyatTakip.Services.MarketPrices;

/// <summary>
/// Marketlerin JSON API'lerine direkt istek atar.
/// HTML scraping yok, tarayıcı yok.
/// </summary>
public partial class LiveMarketPriceProvider(
    HttpClient httpClient,
    ILogger<LiveMarketPriceProvider> logger,
    PlaywrightPageFetcher pageFetcher,
    Adapters.MacrocenterPriceAdapter macrocenterAdapter,
    Adapters.MigrosPriceAdapter migrosAdapter,
    Adapters.CarrefourSAPriceAdapter carrefourSAAdapter,
    Adapters.GenericNextJsPriceAdapter nextJsAdapter,
    Routing.IMarketAdapterExecutor adapterExecutor) : IMarketPriceProvider
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
            .SelectMany(v => v.DirectLinks.Where(l => l.IsActive && !string.IsNullOrEmpty(l.DirectUrl)))
            .Select(l => l.MarketId)
            .Distinct()
            .Where(id => !searchMarketIds.Contains(id))
            .ToHashSet();

        var allMarkets = markets
            .Where(m => searchMarketIds.Contains(m.Id) || directLinkMarketIds.Contains(m.Id))
            .ToList();

        // Aynı anda en fazla 3 market isteği
        using var throttle = new SemaphoreSlim(3, 3);

        var tasks = allMarkets.Select(async m =>
        {
            // Bu markete link'i olan çeşitlerin search target'larını filtrele (req 1)
            var linkedVarietyIds = product.Varieties
                .SelectMany(v => v.DirectLinks.Where(l => l.MarketId == m.Id && l.IsActive))
                .Select(l => l.ProductVarietyId)
                .ToHashSet();

            var effectiveTargets = targets;
            if (linkedVarietyIds.Count > 0)
            {
                var filtered = targets
                    .Where(t => !t.VarietyId.HasValue || linkedVarietyIds.Contains(t.VarietyId.Value))
                    .ToList();
                if (filtered.Count > 0) effectiveTargets = filtered;
            }

            await throttle.WaitAsync(cancellationToken);
            try
            {
                return await FetchMarketAsync(product, m, effectiveTargets, cancellationToken);
            }
            finally
            {
                throttle.Release();
            }
        });

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
                .SelectMany(v => v.DirectLinks.Where(l => l.MarketId == market.Id && l.IsActive && !string.IsNullOrEmpty(l.DirectUrl)))
                .ToList();

            foreach (var dl in directLinks)
            {
                var dTarget = targets.FirstOrDefault(t => t.VarietyId == dl.ProductVarietyId)
                              ?? targets.FirstOrDefault();
                if (dTarget is null) continue;

                // ── ADIM 0.1: Dynamic Executor (Platform-Based) ──
                var variety = product.Varieties.FirstOrDefault(v => v.Id == dTarget.VarietyId);
                var executorResult = await adapterExecutor.TryFetchDirectAsync(
                    market, dl.DirectUrl!, product, variety, cts.Token);
                if (executorResult is not null)
                    return executorResult;

                // ── Macrocenter: JSON-LD adapter (ExtractItems zincirinden bağımsız) ──
                if (market.Name.Equals("Macrocenter", StringComparison.OrdinalIgnoreCase))
                {
                    var mcResult = await macrocenterAdapter.TryFetchAsync(
                        market, dl.DirectUrl!, product.Name, dTarget.VarietyName,
                        dTarget.VarietyId, product.Unit, cts.Token);
                    if (mcResult is not null)
                        return mcResult;
                    logger.LogInformation("{Market}: Adapter başarısız, genel zincir devreye giriyor.", market.Name);
                }

                // ── Migros: JSON API adapter (DirectUrl) ─────────────────────────────
                if (market.Name.Equals("Migros", StringComparison.OrdinalIgnoreCase))
                {
                    var mgDirectResult = await migrosAdapter.TryFetchDirectAsync(
                        market, dl.DirectUrl!, product.Name, dTarget.VarietyName,
                        dTarget.VarietyId, product.Unit, cts.Token);
                    if (mgDirectResult is not null)
                        return mgDirectResult;
                    logger.LogInformation("{Market}: Adapter (direct) başarısız, genel zincir devreye giriyor.", market.Name);
                }

                // ── CarrefourSA: SAP Commerce / JSON-LD adapter (DirectUrl) ──────────
                if (market.Name.Equals("CarrefourSA", StringComparison.OrdinalIgnoreCase))
                {
                    var csaDirectResult = await carrefourSAAdapter.TryFetchDirectAsync(
                        market, dl.DirectUrl!, product.Name, dTarget.VarietyName,
                        dTarget.VarietyId, product.Unit, cts.Token);
                    if (csaDirectResult is not null)
                        return csaDirectResult;
                    logger.LogInformation("{Market}: CSA Adapter (direct) başarısız, genel zincir devreye giriyor.", market.Name);
                }

                // ── Generic Next.js: NextData adapter (DirectUrl) ───────────────────
                var nextJsDirectResult = await nextJsAdapter.TryFetchDirectAsync(
                    market, dl.DirectUrl!, product.Name, dTarget.VarietyName,
                    dTarget.VarietyId, product.Unit, cts.Token);
                if (nextJsDirectResult is not null)
                    return nextJsDirectResult;

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
                    var pwDirImg = pwDirBest.ImageUrl ?? ExtractHtmlImage(pwHtml, market.BaseUrl);
                    return new MarketPriceResult(
                        market.Id, market.Name,
                        pwDirBest.Price, pwDirBest.Url ?? dl.DirectUrl, ProviderName,
                        IsLive: true, ProductVarietyId: dTarget.VarietyId,
                        MatchedTitle: pwDirBest.Title, ImageUrl: pwDirImg,
                        ConfidenceScore: Math.Max(pwDirBest.Score, 75));
                }
            }

            // Direkt URL bulunamadı; arama URL'i yoksa çık
            if (string.IsNullOrWhiteSpace(market.SearchUrlTemplate))
                return null;

            // ── ADIM 0.2: Dynamic Executor (Platform-Based Search) ──
            foreach (var target in targets.Take(2))
            {
                // SearchTarget'tan variety'i bul
                var variety = product.Varieties.FirstOrDefault(v => v.Id == target.VarietyId);
                var executorSearchResult = await adapterExecutor.TryFetchSearchAsync(
                    market, product, variety, cts.Token);
                if (executorSearchResult is not null)
                    return executorSearchResult;
            }

            // ── Migros: JSON API adapter (Search) ──────────────────────────────────
            if (market.Name.Equals("Migros", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var target in targets.Take(2))
                {
                    var mgSearchResult = await migrosAdapter.TryFetchSearchAsync(
                        market, target.Query, product.Name, target.VarietyName,
                        target.VarietyId, product.Unit, cts.Token);
                    if (mgSearchResult is not null)
                        return mgSearchResult;
                }
                logger.LogInformation("{Market}: Adapter (search) başarısız, genel zincir devreye giriyor.", market.Name);
            }

            // ── CarrefourSA: SAP Commerce adapter (Search) ───────────────────────
            if (market.Name.Equals("CarrefourSA", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var target in targets.Take(2))
                {
                    var csaSearchResult = await carrefourSAAdapter.TryFetchSearchAsync(
                        market, target.Query, product.Name, target.VarietyName,
                        target.VarietyId, product.Unit, cts.Token);
                    if (csaSearchResult is not null)
                        return csaSearchResult;
                }
                logger.LogInformation("{Market}: CSA Adapter (search) başarısız, genel zincir devreye giriyor.", market.Name);
            }

            // ── Generic Next.js: NextData adapter (Search) ────────────────────────
            foreach (var target in targets.Take(2))
            {
                var nextJsSearchResult = await nextJsAdapter.TryFetchSearchAsync(
                    market, target.Query, product.Name, target.VarietyName,
                    target.VarietyId, product.Unit, cts.Token);
                if (nextJsSearchResult is not null)
                    return nextJsSearchResult;
            }

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
                        var pwImg = pwBest.ImageUrl ?? ExtractHtmlImage(rendered, market.BaseUrl);
                        return new MarketPriceResult(
                            market.Id, market.Name,
                            pwBest.Price, pwBest.Url ?? url, ProviderName,
                            IsLive: true, ProductVarietyId: target.VarietyId,
                            MatchedTitle: pwBest.Title, ImageUrl: pwImg, ConfidenceScore: pwBest.Score);
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

                // WooCommerce link yok → NopCommerce arama sayfasından ürün linkleri çıkar
                if (productLinks.Count == 0 && rendered is not null && NopCommercePriceExtractor.IsMatch(rendered))
                {
                    var nopLinks = ExtractNopCommerceLinks(rendered, market.BaseUrl);
                    if (nopLinks.Count > 0)
                    {
                        logger.LogInformation("{Market}: {N} NopCommerce ürün linki bulundu. Query: {Q}", market.Name, nopLinks.Count, target.Query);
                        foreach (var productUrl in nopLinks.Take(5))
                        {
                            var (result, _) = await FetchOneAsync(product, market, target, productUrl, cts.Token);
                            if (result is not null)
                                return result;
                        }
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

            // isPureJson = supports retry; hasEmbeddedJson = one-shot only
            bool hasJson = isPureJson;

            var items = ExtractItems(body, market, url);

            // Saf HTML (JSON/NextData/JSON-LD yok) ve HTML extractor da boş döndüyse atla
            if (!isPureJson && !hasEmbeddedJson && items.Count == 0)
            {
                logger.LogInformation("{Market}: HTML yanıt (JSON/HTML yok), atlanıyor. Query: {Q}", market.Name, target.Query);
                return (null, false);
            }

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

            // Resim yoksa HTML'den og:image gibi meta etiketleri dene
            var imgUrl = best.ImageUrl ?? (!isPureJson ? ExtractHtmlImage(body, market.BaseUrl) : null);

            return (new MarketPriceResult(
                market.Id, market.Name,
                best.Price,
                best.Url ?? url,
                ProviderName,
                IsLive: true,
                ProductVarietyId: target.VarietyId,
                MatchedTitle: best.Title,
                ImageUrl: imgUrl,
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

        // 4. Fallback: platform-specific HTML extractors (JSON bulunamadıysa)
        if (items.Count == 0)
        {
            var htmlCandidates =
                NopCommercePriceExtractor.TryExtract(body, sourceUrl, market.BaseUrl)
                ?? WooCommercePriceExtractor.TryExtract(body, sourceUrl, market.BaseUrl);

            if (htmlCandidates is not null)
                items.AddRange(htmlCandidates.Select(c =>
                    new Candidate(c.Title, c.Price, c.Url, c.ImageUrl, c.InStock)));
        }

        // 5. Ticimax fallback (Taze Dükkan vb.): productDetailModel JS değişkeninden fiyat çıkar
        if (items.Count == 0)
        {
            var tc = ExtractTicimax(body, sourceUrl, market.BaseUrl);
            if (tc is not null)
                items.Add(tc);
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

    private static readonly Dictionary<string, (decimal Min, decimal Max)> ProductPriceRange =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // (min TL/kg, max TL/kg) — piyasa gerçekçi aralıkları
            ["Mandalina"]       = (15m,  350m),
            ["Limon"]           = (20m,  400m),
            ["Lime"]            = (40m,  600m),   // Şok 19.95 gibi yanlış eşleşmeleri engeller
            ["Finger Lime"]     = (100m, 2000m),
            ["Portakal"]        = (15m,  350m),
            ["Avokado"]         = (40m,  600m),
            ["Nar"]             = (20m,  400m),
            ["Ejder Meyvesi"]   = (50m,  800m),
            ["Çarkıfelek"]      = (30m,  600m),
            ["Mango"]           = (40m,  800m),
            ["Domates"]         = (10m,  300m),
            ["Limon Otu"]       = (8m,   200m),
        };

    private static Candidate? PickBest(List<Candidate> candidates, Product product, SearchTarget target)
    {
        var prodNorm    = N(product.Name);
        var queryNorm   = N(target.Query);
        var varietyNorm = N(target.VarietyName);

        var (minPrice, maxPrice) = ProductPriceRange.TryGetValue(product.Name, out var r)
            ? r
            : (IsGrocery(product) ? 12m : 5m, 4000m);

        return candidates
            .Where(c => c.InStock)
            .Where(c => c.Price >= minPrice && c.Price <= maxPrice)
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
        if (ContainsTurkishWord(hay, prodNorm))                                       s += 40;
        if (prodNorm != varietyNorm && ContainsTurkishWord(hay, varietyNorm))         s += 25;
        if (prodNorm != queryNorm   && ContainsPhrase(hay, queryNorm))                s += 20;
        if (Any(hay, "kg", "gr", "adet", "taze", "organik", "meyve", "sebze"))       s += 10;
        if (Any(hay, "stok yok", "tukendi", "satis disi"))                            s -= 30;
        return Math.Clamp(s, 0, 100);
    }

    private static bool IsRelevant(string title, string prodNorm, string queryNorm)
    {
        var n = N(title);
        // Ürün adı: Türkçe ek toleranslı kelime eşleşmesi
        if (ContainsTurkishWord(n, prodNorm)) return true;
        // Sorgu: tam phrase
        if (ContainsPhrase(n, queryNorm)) return true;
        // Sorgu: çok kelimeli ise her kelime ayrı ayrı geçiyorsa da relevant say
        // ("washington portakal" → "portakali" + "washington" ayrı ayrı)
        if (queryNorm.Contains(' '))
        {
            var parts = queryNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.All(p => ContainsTurkishWord(n, p))) return true;
        }
        return false;
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

    // Türkçe ek toleranslı kelime eşleşmesi.
    // Kök >= 4 karakter ise sonunda en fazla 5 harflik Türkçe ek kabul edilir.
    // "portakal" → "portakali" ✓  |  "nar" → "narenciye" ✗ (kök < 4 → ContainsWord)
    private static bool ContainsTurkishWord(string hay, string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        if (word.Length < 4) return ContainsWord(hay, word);

        var idx = hay.IndexOf(word, StringComparison.Ordinal);
        while (idx >= 0)
        {
            bool startOk = idx == 0 || !char.IsLetter(hay[idx - 1]);
            if (startOk)
            {
                int afterIdx = idx + word.Length;
                // Tam kelime eşleşmesi
                if (afterIdx >= hay.Length || !char.IsLetter(hay[afterIdx]))
                    return true;
                // Türkçe ek: ardından gelen en fazla 5 harf, sonra harf değil ya da string sonu
                int suf = 0;
                while (afterIdx + suf < hay.Length && char.IsLetter(hay[afterIdx + suf]) && suf < 5)
                    suf++;
                bool endOk = afterIdx + suf >= hay.Length || !char.IsLetter(hay[afterIdx + suf]);
                if (endOk && suf is >= 1 and <= 5)
                    return true;
            }
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

    // Ticimax platformu (Taze Dükkan vb.): productDetailModel JS değişkeninden ürün adı ve fiyat çıkar
    private static Candidate? ExtractTicimax(string html, string sourceUrl, string? baseUrl)
    {
        if (!html.Contains("productDetailModel", StringComparison.Ordinal)) return null;

        var nameM = TicimaxNameRegex().Match(html);
        if (!nameM.Success) return null;
        var name = WebUtility.HtmlDecode(nameM.Groups["n"].Value.Trim());

        // Fiyat: önce indirimliFiyati (aktif kampanya), yoksa satisFiyati
        decimal price = 0;
        var indirimM = TicimaxIndirimliRegex().Match(html);
        if (indirimM.Success
            && decimal.TryParse(indirimM.Groups["p"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var indirim)
            && indirim >= 5)
            price = indirim;

        if (price <= 0)
        {
            var satisM = TicimaxSatisRegex().Match(html);
            if (satisM.Success && decimal.TryParse(satisM.Groups["p"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var satis))
                price = satis;
        }

        if (price <= 0 || price > 9999) return null;

        // Stok: stokAdedi > 0 → stokta; bulunamazsa true (aktif sayfa = satışta varsayım)
        var stockM = TicimaxStokRegex().Match(html);
        bool inStock = !stockM.Success
            || (int.TryParse(stockM.Groups["s"].Value, out var stok) && stok > 0);

        return new Candidate(Clean(name), decimal.Round(price, 2), sourceUrl, ExtractHtmlImage(html, baseUrl), inStock);
    }

    // NopCommerce arama sonuç sayfasındaki ürün linklerini çıkar
    private static IReadOnlyList<string> ExtractNopCommerceLinks(string html, string? baseUrl)
    {
        var seen  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var links = new List<string>();
        foreach (Match m in NopCommerceProductLinkRegex().Matches(html))
        {
            var url = Resolve(WebUtility.HtmlDecode(m.Groups["u"].Value), baseUrl);
            if (url is not null && seen.Add(url))
                links.Add(url);
        }
        return links;
    }

    // HTML sayfalarından og:image / itemprop meta etiketlerini çek
    private static string? ExtractHtmlImage(string html, string? baseUrl)
    {
        var m = OgImageRegex().Match(html);
        if (!m.Success) m = OgImageRegex2().Match(html);
        if (!m.Success) m = ItemPropImageRegex().Match(html);
        if (!m.Success) m = SchemaImageRegex().Match(html);
        if (!m.Success) return null;
        var raw = System.Net.WebUtility.HtmlDecode(m.Groups["url"].Value.Trim());
        return Resolve(raw, baseUrl);
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

    [GeneratedRegex(@"<meta[^>]+property=[""']og:image[""'][^>]+content=[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex OgImageRegex();

    [GeneratedRegex(@"<meta[^>]+content=[""'](?<url>[^""']+)[""'][^>]+property=[""']og:image[""']", RegexOptions.IgnoreCase)]
    private static partial Regex OgImageRegex2();

    [GeneratedRegex(@"<(?:img|link)[^>]+itemprop=[""']image[""'][^>]+(?:src|href|content)=[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex ItemPropImageRegex();

    [GeneratedRegex(@"""image""\s*:\s*""(?<url>https?://[^""]+\.(jpg|jpeg|png|webp)[^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex SchemaImageRegex();

    // Ticimax: productDetailModel alanları
    [GeneratedRegex(@"""productName""\s*:\s*""(?<n>[^""]+)""")]
    private static partial Regex TicimaxNameRegex();

    [GeneratedRegex(@"""indirimliFiyati""\s*:\s*(?<p>\d+(?:\.\d+)?)")]
    private static partial Regex TicimaxIndirimliRegex();

    [GeneratedRegex(@"""satisFiyati""\s*:\s*(?<p>\d+(?:\.\d+)?)")]
    private static partial Regex TicimaxSatisRegex();

    [GeneratedRegex(@"""stokAdedi""\s*:\s*(?<s>\d+)")]
    private static partial Regex TicimaxStokRegex();

    // NopCommerce arama sayfası ürün linki: <h2 class="product-title"><a href="...">
    [GeneratedRegex(@"<h2[^>]+class=[""']product-title[""'][^>]*>\s*<a[^>]+href=[""'](?<u>[^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NopCommerceProductLinkRegex();

    // ── TYPES ─────────────────────────────────────────────────────────────────

    // DirectUrl test — UrlManagement ekranı için public yardımcı
    public async Task<(bool Found, string Title, decimal Price, string? Error)> TestDirectUrlAsync(string url)
    {
        try
        {
            var baseUrl = new Uri(url).GetLeftPart(UriPartial.Authority);
            using var req  = new HttpRequestMessage(HttpMethod.Get, url);
            SetHeaders(req, baseUrl);
            using var resp = await httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return (false, "", 0m, $"HTTP {(int)resp.StatusCode}");
            var body   = await resp.Content.ReadAsStringAsync();
            var market = new Market { Name = "Test", BaseUrl = baseUrl };
            var items  = ExtractItems(body, market, url);
            var best   = items.FirstOrDefault(c => c.InStock) ?? items.FirstOrDefault();
            if (best == null)
                return (false, "", 0m, "Fiyat bulunamadı");
            return (true, best.Title, best.Price, null);
        }
        catch (Exception ex)
        {
            var msg = ex.Message.Length > 120 ? ex.Message[..120] + "…" : ex.Message;
            return (false, "", 0m, "Sayfa okunamadı: " + msg);
        }
    }

    // ── URL DISCOVERY (public, UrlManagement için) ────────────────────────────
    // SearchUrlTemplate varsa arama sayfasını çekip ürün URL adaylarını döner.
    // (Title, Url, Score=0, HasPriceData) — Score burada 0; DirectUrlDiscoveryService'de hesaplanır.
    public async Task<IReadOnlyList<(string Title, string Url, int Score, bool HasPrice)>>
        FetchSearchLinksAsync(Market market, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(market.SearchUrlTemplate)) return [];

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(12));

        var searchUrl = BuildUrl(market.SearchUrlTemplate, query);
        logger.LogInformation("{Market}: URL keşfi, query: {Q}", market.Name, query);

        string? html = null;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            SetHeaders(req, market.BaseUrl);
            using var resp = await httpClient.SendAsync(req, cts.Token);
            if (resp.IsSuccessStatusCode)
                html = await resp.Content.ReadAsStringAsync(cts.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogInformation("{Market}: URL keşfi HttpClient başarısız: {Msg}", market.Name, ex.Message);
        }

        if (html is null && pageFetcher.IsAvailable)
        {
            try
            {
                var (rendered, wooLinks) = await pageFetcher.FetchWithLinksAsync(
                    searchUrl, "a.woocommerce-loop-product__link", cts.Token);
                html = rendered;

                if (wooLinks.Count > 0)
                    return wooLinks.Take(8).Select(u => ("", u, 0, false)).ToList();
            }
            catch (Exception ex)
            {
                logger.LogInformation("{Market}: URL keşfi Playwright başarısız: {Msg}", market.Name, ex.Message);
            }
        }

        if (html is null) return [];

        var items = ExtractItems(html, market, searchUrl);
        if (items.Count > 0)
            return items.Take(8).Select(c => (c.Title, c.Url, c.Score, c.Price > 0)).ToList();

        if (NopCommercePriceExtractor.IsMatch(html))
        {
            var links = ExtractNopCommerceLinks(html, market.BaseUrl);
            return links.Take(8).Select(u => ("", u, 0, false)).ToList();
        }

        return [];
    }

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
