using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

public class LiveMarketPriceProvider(
    HttpClient httpClient,
    PlaywrightPageFetcher playwrightFetcher,
    ILogger<LiveMarketPriceProvider> logger) : IMarketPriceProvider
{
    private const int MinimumConfidenceScore = 60;

    public string ProviderName => "LiveHttp";

    public async Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(
        Product product,
        IReadOnlyList<Market> markets,
        CancellationToken cancellationToken = default)
    {
        var targets = BuildSearchTargets(product);

        var tasks = markets
            .Where(market => market.IsActive && !string.IsNullOrWhiteSpace(market.SearchUrlTemplate))
            .Select(market => TryGetMarketPriceAsync(product, market, targets, cancellationToken));

        var allResults = await Task.WhenAll(tasks);

        return allResults
            .Where(result => result is not null)
            .Select(result => result!)
            .OrderBy(result => result.MarketName)
            .ToList();
    }

    private async Task<MarketPriceResult?> TryGetMarketPriceAsync(
        Product product,
        Market market,
        IReadOnlyList<SearchTarget> targets,
        CancellationToken cancellationToken)
    {
        foreach (var target in targets.Take(3))
        {
            var searchUrl = BuildSearchUrl(market, target.Query);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                AddBrowserHeaders(request, market.BaseUrl);

                using var response = await httpClient.SendAsync(request, cancellationToken);

                string content;

                if (!response.IsSuccessStatusCode)
                {
                    // 403/blocked → Playwright ile dene
                    if (playwrightFetcher.IsAvailable &&
                        ((int)response.StatusCode == 403 || (int)response.StatusCode == 429))
                    {
                        var rendered = await playwrightFetcher.FetchAsync(searchUrl, cancellationToken);
                        if (rendered is null || rendered.Length < 5_000)
                        {
                            logger.LogWarning(
                                "{Market} okunamadi (Playwright da basarisiz). Status: {StatusCode}",
                                market.Name,
                                response.StatusCode);
                            continue;
                        }
                        content = rendered;
                    }
                    else
                    {
                        logger.LogWarning(
                            "{Market} okunamadi. Status: {StatusCode}. Url: {Url}",
                            market.Name,
                            response.StatusCode,
                            searchUrl);
                        continue;
                    }
                }
                else
                {
                    content = await response.Content.ReadAsStringAsync(cancellationToken);
                }

                // Sayfa çok küçükse (JS SPA shell) → direkt Playwright dene
                if (content.Length < 8_000 && playwrightFetcher.IsAvailable)
                {
                    var rendered = await playwrightFetcher.FetchAsync(searchUrl, cancellationToken);
                    if (rendered is not null && rendered.Length > content.Length)
                    {
                        content = rendered;
                    }
                }

                var candidates = ExtractCandidates(content, market, searchUrl);

                // Büyük sayfa geldi ama ürün bulunamadı → Playwright ile JS render edilmiş hali dene
                if (candidates.Count == 0 && content.Length >= 8_000 && playwrightFetcher.IsAvailable)
                {
                    var rendered = await playwrightFetcher.FetchAsync(searchUrl, cancellationToken);
                    if (rendered is not null && rendered.Length > 5_000)
                    {
                        var playwrightCandidates = ExtractCandidates(rendered, market, searchUrl);
                        if (playwrightCandidates.Count > 0)
                        {
                            candidates = playwrightCandidates;
                        }
                    }
                }

                var bestCandidate = FindBestCandidate(candidates, product, target);

                if (bestCandidate is null)
                {
                    logger.LogWarning(
                        "{Market} icin uygun canli urun bulunamadi. Query: {Query}",
                        market.Name,
                        target.Query);

                    continue;
                }

                return new MarketPriceResult(
                    market.Id,
                    market.Name,
                    bestCandidate.Price.GetValueOrDefault(),
                    bestCandidate.Url ?? searchUrl,
                    ProviderName,
                    IsLive: true,
                    ProductVarietyId: target.ProductVarietyId,
                    MatchedTitle: bestCandidate.Title,
                    ImageUrl: bestCandidate.ImageUrl,
                    ConfidenceScore: bestCandidate.ConfidenceScore);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException or JsonException)
            {
                logger.LogWarning(ex, "{Market} icin canli fiyat cekilemedi. Query: {Query}", market.Name, target.Query);
            }
        }

        return null;
    }

    private static IReadOnlyList<SearchTarget> BuildSearchTargets(Product product)
    {
        var varietyTargets = product.Varieties
            .Where(variety => variety.IsActive)
            .SelectMany(variety =>
                variety.SearchAliases
                    .OrderBy(alias => alias.Priority)
                    .Take(1)
                    .Select(alias => new SearchTarget(
                        variety.Id,
                        alias.Query,
                        $"{variety.Name} {product.Name}",
                        variety.Name,
                        product.Name)))
            .ToList();

        if (varietyTargets.Count > 0)
        {
            return varietyTargets;
        }

        return
        [
            new SearchTarget(
                null,
                product.Name,
                product.Name,
                product.Name,
                product.Name)
        ];
    }

    private static void AddBrowserHeaders(HttpRequestMessage request, string? baseUrl)
    {
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        request.Headers.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,application/json;q=0.8,*/*;q=0.7");

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            request.Headers.Referrer = new Uri(baseUrl);
        }
    }

    private static string BuildSearchUrl(Market market, string productName)
    {
        var encodedProduct = WebUtility.UrlEncode(productName);

        return string.Format(
            CultureInfo.InvariantCulture,
            market.SearchUrlTemplate!,
            encodedProduct);
    }

    private static List<ProductCandidate> ExtractCandidates(string content, Market market, string sourceUrl)
    {
        var candidates = new List<ProductCandidate>();

        if (LooksLikeJson(content))
        {
            ExtractFromJson(content, candidates, market, sourceUrl);
        }

        foreach (Match match in ScriptJsonRegex().Matches(content))
        {
            var json = WebUtility.HtmlDecode(match.Groups["json"].Value.Trim());
            ExtractFromJson(json, candidates, market, sourceUrl);
        }

        foreach (Match match in NextDataRegex().Matches(content))
        {
            var json = WebUtility.HtmlDecode(match.Groups["json"].Value.Trim());
            ExtractFromJson(json, candidates, market, sourceUrl);
        }

        ExtractFromHtmlBlocks(content, candidates, market, sourceUrl);

        return candidates
            .Where(candidate => candidate.Price.HasValue)
            .GroupBy(candidate => NormalizeTurkish(candidate.Title) + "|" + candidate.Price)
            .Select(group => group.First())
            .ToList();
    }

    private static ProductCandidate? FindBestCandidate(
        List<ProductCandidate> candidates,
        Product product,
        SearchTarget target)
    {
        foreach (var candidate in candidates)
        {
            candidate.ConfidenceScore = CalculateConfidence(candidate, product, target);
        }

        var minPrice = IsFreshProduceProduct(product) ? 10m : 5m;

        return candidates
            .Where(candidate =>
                candidate.Price >= minPrice &&
                candidate.Price <= 1500 &&
                candidate.IsInStock &&
                candidate.ConfidenceScore >= MinimumConfidenceScore &&
                IsLikelyFreshProduceCandidate(candidate, product) &&
                !LooksLikeWrongProduct(candidate.Title, product, target))
            .OrderByDescending(candidate => candidate.ConfidenceScore)
            .ThenBy(candidate => candidate.Price)
            .FirstOrDefault();
    }

    private static bool LooksLikeJson(string content)
    {
        var trimmed = content.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private static void ExtractFromJson(
        string json,
        List<ProductCandidate> candidates,
        Market market,
        string sourceUrl)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            WalkJson(document.RootElement, candidates, market, sourceUrl);
        }
        catch (JsonException)
        {
            // HTML icindeki her script gecerli JSON olmayabilir. Sessizce atlanir.
        }
    }

    private static void WalkJson(
        JsonElement element,
        List<ProductCandidate> candidates,
        Market market,
        string sourceUrl)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var candidate = CandidateFromJsonObject(element, market, sourceUrl);

            if (candidate is not null)
            {
                candidates.Add(candidate);
            }

            foreach (var property in element.EnumerateObject())
            {
                WalkJson(property.Value, candidates, market, sourceUrl);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                WalkJson(item, candidates, market, sourceUrl);
            }
        }
    }

    private static ProductCandidate? CandidateFromJsonObject(JsonElement element, Market market, string sourceUrl)
    {
        var title = FirstString(element,
            "name", "title", "productName", "displayName", "shortName", "seoName", "item_name");

        var price = FirstPrice(element,
            "price", "salePrice", "discountedPrice", "unitPrice", "shownPrice", "currentPrice", "amount", "value");

        if (string.IsNullOrWhiteSpace(title) || !price.HasValue)
        {
            return null;
        }

        var stockText = FirstString(element,
            "stockStatus", "stockText", "availability", "availableForSale", "isAvailable", "inStock", "hasStock");

        var explicitStock = FirstBool(element, "inStock", "isInStock", "available", "isAvailable", "hasStock", "availableForSale");
        var isInStock = explicitStock ?? !IsOutOfStockText(stockText ?? title);

        return new ProductCandidate
        {
            Title = CleanText(title),
            Price = price.Value,
            Url = ResolveUrl(FirstString(element, "url", "productUrl", "link", "seoUrl"), market.BaseUrl) ?? sourceUrl,
            ImageUrl = ResolveUrl(FirstString(element, "image", "imageUrl", "picture", "pictureUrl", "thumbnail", "thumbnailUrl"), market.BaseUrl),
            IsInStock = isInStock,
            RawText = element.ToString()
        };
    }

    private static void ExtractFromHtmlBlocks(
        string html,
        List<ProductCandidate> candidates,
        Market market,
        string sourceUrl)
    {
        foreach (Match match in ProductBlockRegex().Matches(html))
        {
            var block = match.Value;
            var title = FindTitleInHtml(block);
            var price = FindPrice(block);

            if (string.IsNullOrWhiteSpace(title) || !price.HasValue)
            {
                continue;
            }

            candidates.Add(new ProductCandidate
            {
                Title = CleanText(title),
                Price = price.Value,
                Url = ResolveUrl(FindHref(block), market.BaseUrl) ?? sourceUrl,
                ImageUrl = ResolveUrl(FindImage(block), market.BaseUrl),
                IsInStock = !IsOutOfStockText(block) && HasPossibleBuySignal(block),
                RawText = block
            });
        }
    }

    private static string? FindTitleInHtml(string html)
    {
        foreach (var regex in new[] { ProductNameAttributeRegex(), AltRegex(), AriaLabelRegex(), HTagRegex(), TitleAttributeRegex() })
        {
            var match = regex.Match(html);

            if (match.Success)
            {
                return WebUtility.HtmlDecode(StripTags(match.Groups["title"].Value));
            }
        }

        return null;
    }

    private static decimal? FindPrice(string text)
    {
        var prices = new List<decimal>();

        foreach (Match match in JsonPriceRegex().Matches(text))
        {
            if (TryParsePrice(match.Groups["price"].Value, out var price))
            {
                prices.Add(price);
            }
        }

        foreach (Match match in TurkishPriceRegex().Matches(text))
        {
            if (TryParsePrice(match.Groups["price"].Value, out var price))
            {
                prices.Add(price);
            }
        }

        var valid = prices.Where(p => p >= 5 && p <= 1500).OrderBy(p => p).ToList();
        return valid.Count > 0 ? valid[0] : null;
    }

    private static string? FindHref(string html)
    {
        var match = HrefRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["url"].Value) : null;
    }

    private static string? FindImage(string html)
    {
        var match = ImageRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["url"].Value) : null;
    }

    private static int CalculateConfidence(ProductCandidate candidate, Product product, SearchTarget target)
    {
        var haystack = NormalizeTurkish($"{candidate.Title} {candidate.RawText}");
        var productName = NormalizeTurkish(product.Name);
        var varietyName = NormalizeTurkish(target.VarietyName);
        var query = NormalizeTurkish(target.Query);

        var score = 0;

        if (haystack.Contains(productName))
        {
            score += 45;
        }

        if (!string.Equals(productName, varietyName, StringComparison.OrdinalIgnoreCase) && haystack.Contains(varietyName))
        {
            score += 25;
        }

        if (!string.Equals(productName, query, StringComparison.OrdinalIgnoreCase) && haystack.Contains(query))
        {
            score += 20;
        }

        if (ContainsAny(haystack, "kg", "kilogram", "gr", "gram", "meyve", "sebze", "narenciye", "adet", "taze", "organik"))
        {
            score += 10;
        }

        if (candidate.IsInStock)
        {
            score += 10;
        }

        if (LooksLikeWrongProduct(candidate.Title, product, target))
        {
            score -= 40;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static bool LooksLikeWrongProduct(string title, Product product, SearchTarget target)
    {
        var normalizedTitle = NormalizeTurkish(title);
        var productName = NormalizeTurkish(product.Name);
        var query = NormalizeTurkish(target.Query);

        if (normalizedTitle.Contains("anasayfa") || normalizedTitle.Contains("home") || normalizedTitle.Contains("sepet"))
        {
            return true;
        }

        if (IsFreshProduceProduct(product) && ContainsAny(normalizedTitle,
            "fanta",
            "gazoz",
            "icecek",
            "meyve suyu",
            "nektar",
            "soda",
            "kola",
            "aromali",
            "surup",
            "toz icecek",
            "cikolata",
            "biskuvi",
            "biskuit",
            "kek",
            "recel",
            "marmelat",
            "tatli",
            "konserve",
            "dondurma",
            "cool",
            "sprite",
            "schweppes",
            "pepsi",
            "lipton",
            "cappy",
            "dimes",
            "tamek",
            "pinar",
            "enerji",
            "energy",
            "buzlu",
            "gazli",
            " ml",
            "sise",
            "kutu",
            "teneke",
            "cepte sok"))
        {
            return true;
        }

        return !normalizedTitle.Contains(productName) && !normalizedTitle.Contains(query);
    }

    private static bool IsLikelyFreshProduceCandidate(ProductCandidate candidate, Product product)
    {
        if (!IsFreshProduceProduct(product))
        {
            return true;
        }

        var normalized = NormalizeTurkish($"{candidate.Title} {candidate.RawText}");

        if (ContainsAny(normalized,
            "fanta",
            "gazoz",
            "icecek",
            "meyve suyu",
            "nektar",
            "soda",
            "kola",
            "aromali",
            "surup",
            "toz icecek",
            "cikolata",
            "biskuvi",
            "biskuit",
            "kek",
            "recel",
            "marmelat",
            "tatli",
            "konserve",
            "dondurma",
            "cool",
            "sprite",
            "schweppes",
            "pepsi",
            "lipton",
            "cappy",
            "dimes",
            "tamek",
            "pinar",
            "enerji",
            "energy",
            "buzlu",
            "gazli",
            " ml",
            "sise",
            "kutu",
            "teneke",
            "cepte sok"))
        {
            return false;
        }

        return ContainsAny(normalized,
            "kg",
            "kilogram",
            "gr",
            "gram",
            "adet",
            "meyve",
            "sebze",
            "narenciye",
            "organik",
            "taze");
    }

    private static bool IsFreshProduceProduct(Product product)
    {
        return ContainsAny($"{product.Category} {product.Name}",
            "narenciye",
            "tropikal",
            "mandalina",
            "limon",
            "lime",
            "portakal",
            "avokado",
            "greyfurt",
            "kumkuat",
            "bergamot");
    }

    private static bool HasPossibleBuySignal(string text)
    {
        var normalized = NormalizeTurkish(text);

        return ContainsAny(normalized,
            "sepete ekle",
            "add to cart",
            "addtocart",
            "satisa acik",
            "available",
            "stokta",
            "in stock",
            "price",
            "tl",
            "â‚º");
    }

    private static bool IsOutOfStockText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = NormalizeTurkish(text);

        return ContainsAny(normalized,
            "stokta yok",
            "stok yok",
            "tukendi",
            "tukenmistir",
            "gelince haber ver",
            "satis disi",
            "satisa kapali",
            "urun bulunamadi",
            "bulunamadi",
            "out of stock",
            "not available",
            "unavailable");
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(needle => value.Contains(NormalizeTurkish(needle)));
    }

    private static string? FirstString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetPropertyIgnoreCase(element, name, out var property))
            {
                if (property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }

                if (property.ValueKind == JsonValueKind.Number || property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
                {
                    return property.ToString();
                }

                if (property.ValueKind == JsonValueKind.Object)
                {
                    var nested = FirstString(property, "text", "label", "name", "value", "formattedValue");

                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }
        }

        return null;
    }

    private static decimal? FirstPrice(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetPropertyIgnoreCase(element, name, out var property))
            {
                var price = PriceFromElement(property);

                if (price.HasValue)
                {
                    return price.Value;
                }
            }
        }

        return null;
    }

    private static decimal? PriceFromElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var numericPrice))
        {
            return NormalizePrice(numericPrice);
        }

        if (element.ValueKind == JsonValueKind.String && TryParsePrice(element.GetString() ?? string.Empty, out var stringPrice))
        {
            return stringPrice;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            var nested = FirstPrice(element, "value", "amount", "price", "salePrice", "discountedPrice", "formattedValue");

            if (nested.HasValue)
            {
                return nested.Value;
            }
        }

        return null;
    }

    private static bool? FirstBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetPropertyIgnoreCase(element, name, out var property))
            {
                if (property.ValueKind == JsonValueKind.True)
                {
                    return true;
                }

                if (property.ValueKind == JsonValueKind.False)
                {
                    return false;
                }

                if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var item in element.EnumerateObject())
            {
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    property = item.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static bool TryParsePrice(string value, out decimal price)
    {
        value = WebUtility.HtmlDecode(value)
            .Trim()
            .Replace("â‚º", string.Empty)
            .Replace("TL", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("TRY", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .Replace(',', '.');

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out price) && price > 0)
        {
            price = NormalizePrice(price);
            return price > 0;
        }

        return false;
    }

    private static decimal NormalizePrice(decimal price)
    {
        // Bazi JSON endpointleri kurus cinsinden fiyat donebilir: 4990 => 49.90
        if (price > 1500 && price % 1 == 0)
        {
            return decimal.Round(price / 100, 2);
        }

        return decimal.Round(price, 2);
    }

    private static string CleanText(string value)
    {
        return Regex.Replace(
                StripTags(WebUtility.HtmlDecode(value)),
                "\\s+",
                " ")
            .Trim();
    }

    private static string StripTags(string value)
    {
        return Regex.Replace(value, "<.*?>", " ");
    }

    private static string? ResolveUrl(string? url, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        url = WebUtility.HtmlDecode(url.Trim());

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(new Uri(baseUrl), url, out var resolvedUri))
        {
            return resolvedUri.ToString();
        }

        return null;
    }

    private static string NormalizeTurkish(string value)
    {
        return WebUtility.HtmlDecode(value)
            .ToLowerInvariant()
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ö", "o")
            .Replace("ç", "c")
            .Replace("Ä±", "i")
            .Replace("ÄŸ", "g")
            .Replace("Ã¼", "u")
            .Replace("ÅŸ", "s")
            .Replace("Ã¶", "o")
            .Replace("Ã§", "c");
    }

    private static Regex ScriptJsonRegex() => new(
        "<script[^>]+type=[\\\"']application/ld\\+json[\\\"'][^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static Regex NextDataRegex() => new(
        "<script[^>]+id=[\\\"']__NEXT_DATA__[\\\"'][^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static Regex ProductBlockRegex() => new(
        "<(?:div|li|article|section)[^>]*(?:product|urun|card|item|sku|tile)[^>]*>.*?</(?:div|li|article|section)>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static Regex JsonPriceRegex() => new(
        "[\\\"'](?:price|salePrice|discountedPrice|unitPrice|currentPrice|amount|value)[\\\"']\\s*:\\s*[\\\"']?(?<price>\\d{1,7}(?:[\\.,]\\d{1,2})?)[\\\"']?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Regex TurkishPriceRegex() => new(
        "(?<price>\\d{1,5}(?:[\\.,]\\d{1,2})?)\\s*(?:TL|TRY|â‚º)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Regex ProductNameAttributeRegex() => new(
        "(?:data-product-name|data-name|product-name|productName)=[\\\"'](?<title>[^\\\"']{2,180})[\\\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Regex AltRegex() => new(
        "alt=[\\\"'](?<title>[^\\\"']{2,180})[\\\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Regex AriaLabelRegex() => new(
        "aria-label=[\\\"'](?<title>[^\\\"']{2,180})[\\\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Regex HTagRegex() => new(
        "<h[1-6][^>]*>(?<title>.*?)</h[1-6]>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static Regex TitleAttributeRegex() => new(
        "title=[\\\"'](?<title>[^\\\"']{2,180})[\\\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Regex HrefRegex() => new(
        "href=[\\\"'](?<url>[^\\\"']{2,500})[\\\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Regex ImageRegex() => new(
        "(?:src|data-src|content)=[\\\"'](?<url>[^\\\"']+?\\.(?:jpg|jpeg|png|webp)(?:\\?[^\\\"']*)?)[\\\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private sealed record SearchTarget(
        int? ProductVarietyId,
        string Query,
        string DisplayName,
        string VarietyName,
        string ProductName);

    private sealed class ProductCandidate
    {
        public string Title { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        public string? Url { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsInStock { get; set; } = true;

        public string RawText { get; set; } = string.Empty;

        public int ConfidenceScore { get; set; }
    }
}





