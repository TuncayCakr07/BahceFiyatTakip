using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

public partial class BraveWebSearchPriceProvider(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<BraveWebSearchPriceProvider> logger) : IMarketPriceProvider
{
    private const string BraveEndpoint = "https://api.search.brave.com/res/v1/web/search";

    public string ProviderName => "BraveWebSearch";

    public async Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(Product product, IReadOnlyList<Market> markets, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("BRAVE_SEARCH_API_KEY")
            ?? configuration["SearchApis:Brave:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return [];
        }

        var webMarket = markets.FirstOrDefault(market => market.Name == "Web Arama");
        if (webMarket is null)
        {
            return [];
        }

        var results = new List<MarketPriceResult>();
        var targets = BuildSearchTargets(product);

        foreach (var target in targets.Take(8))
        {
            var query = $"{target.Query} fiyat kg resim";
            var searchUrl = $"{BraveEndpoint}?q={WebUtility.UrlEncode(query)}&count=5&country=TR&search_lang=tr";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                request.Headers.Add("X-Subscription-Token", apiKey);
                request.Headers.Accept.ParseAdd("application/json");

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Brave Search cevap vermedi. Status: {StatusCode}", response.StatusCode);
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                foreach (var item in EnumerateWebResults(document.RootElement).Take(5))
                {
                    var offer = await TryReadOfferPageAsync(item.Url, webMarket, target, cancellationToken);
                    if (offer is not null)
                    {
                        results.Add(offer);
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or UriFormatException)
            {
                logger.LogWarning(ex, "Brave Search ile genel web aramasi basarisiz oldu.");
            }
        }

        return results;
    }

    private async Task<MarketPriceResult?> TryReadOfferPageAsync(string url, Market webMarket, SearchTarget target, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("BahceFiyatTakip/1.0 (+https://localhost)");
        request.Headers.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en;q=0.7");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var price = TryFindPrice(html);
        if (!price.HasValue)
        {
            return null;
        }

        return new MarketPriceResult(
            webMarket.Id,
            webMarket.Name,
            price.Value,
            url,
            ProviderName,
            IsLive: true,
            ProductVarietyId: target.ProductVarietyId,
            MatchedTitle: TryFindTitle(html) ?? target.DisplayName,
            ImageUrl: TryFindImageUrl(html, url),
            ConfidenceScore: CalculateConfidence(html, target));
    }

    private static IEnumerable<WebResult> EnumerateWebResults(JsonElement root)
    {
        if (!root.TryGetProperty("web", out var web) || !web.TryGetProperty("results", out var results))
        {
            yield break;
        }

        foreach (var item in results.EnumerateArray())
        {
            if (item.TryGetProperty("url", out var urlProperty))
            {
                var url = urlProperty.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    yield return new WebResult(url);
                }
            }
        }
    }

    private static IReadOnlyList<SearchTarget> BuildSearchTargets(Product product)
    {
        var targets = product.Varieties
            .Where(variety => variety.IsActive)
            .Select(variety =>
            {
                var query = variety.SearchAliases.OrderBy(alias => alias.Priority).FirstOrDefault()?.Query
                    ?? $"{variety.Name} {product.Name}";

                return new SearchTarget(variety.Id, query, $"{variety.Name} {product.Name}", variety.Name, product.Name);
            })
            .ToList();

        return targets.Count > 0
            ? targets
            : [new SearchTarget(null, product.Name, product.Name, product.Name, product.Name)];
    }

    private static decimal? TryFindPrice(string html)
    {
        foreach (Match match in TurkishPriceRegex().Matches(html))
        {
            if (TryParsePrice(match.Groups["price"].Value, out var price))
            {
                return price;
            }
        }

        foreach (Match match in JsonPriceRegex().Matches(html))
        {
            if (TryParsePrice(match.Groups["price"].Value, out var price))
            {
                return price;
            }
        }

        return null;
    }

    private static bool TryParsePrice(string value, out decimal price)
    {
        value = value.Trim().Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out price) && price > 0;
    }

    private static string? TryFindTitle(string html)
    {
        var match = TitleRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["title"].Value.Trim()) : null;
    }

    private static string? TryFindImageUrl(string html, string sourceUrl)
    {
        foreach (Match match in ImageRegex().Matches(html))
        {
            var imageUrl = WebUtility.HtmlDecode(match.Groups["url"].Value.Trim());
            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
            {
                return imageUrl;
            }

            if (Uri.TryCreate(new Uri(sourceUrl), imageUrl, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }
        }

        return null;
    }

    private static int CalculateConfidence(string html, SearchTarget target)
    {
        var normalized = html.ToLowerInvariant();
        var score = 35;

        if (normalized.Contains(target.ProductName.ToLowerInvariant()))
        {
            score += 20;
        }

        if (normalized.Contains(target.VarietyName.ToLowerInvariant()))
        {
            score += 30;
        }

        if (normalized.Contains("kg") || normalized.Contains("kilogram"))
        {
            score += 15;
        }

        return Math.Min(score, 100);
    }

    [GeneratedRegex("(?<price>\\d{1,5}(?:[\\.,]\\d{1,2})?)\\s*(?:TL|TRY)", RegexOptions.IgnoreCase)]
    private static partial Regex TurkishPriceRegex();

    [GeneratedRegex("\"price\"\\s*:\\s*\"?(?<price>\\d{1,5}(?:[\\.,]\\d{1,2})?)\"?", RegexOptions.IgnoreCase)]
    private static partial Regex JsonPriceRegex();

    [GeneratedRegex("<title[^>]*>(?<title>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("(?:src|data-src|content)=[\"'](?<url>https?://[^\"']+?\\.(?:jpg|jpeg|png|webp)(?:\\?[^\"']*)?)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex ImageRegex();

    private sealed record SearchTarget(int? ProductVarietyId, string Query, string DisplayName, string VarietyName, string ProductName);

    private sealed record WebResult(string Url);
}
