using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices;

public partial class LiveMarketPriceProvider(
    HttpClient httpClient,
    ILogger<LiveMarketPriceProvider> logger) : IMarketPriceProvider
{
    public string ProviderName => "LiveHtml";

    public async Task<IReadOnlyList<MarketPriceResult>> GetPricesAsync(Product product, IReadOnlyList<Market> markets, CancellationToken cancellationToken = default)
    {
        var results = new List<MarketPriceResult>();
        var targets = BuildSearchTargets(product);

        foreach (var market in markets.Where(item => item.IsActive && !string.IsNullOrWhiteSpace(item.SearchUrlTemplate)))
        {
            foreach (var target in targets)
            {
                var searchUrl = BuildSearchUrl(market, target.Query);

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                    request.Headers.UserAgent.ParseAdd("BahceFiyatTakip/1.0 (+https://localhost)");
                    request.Headers.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en;q=0.7");

                    using var response = await httpClient.SendAsync(request, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("{Market} fiyat sayfasi okunamadi. Status: {StatusCode}", market.Name, response.StatusCode);
                        continue;
                    }

                    var html = await response.Content.ReadAsStringAsync(cancellationToken);
                    var price = TryFindPrice(html);
                    if (price.HasValue)
                    {
                        results.Add(new MarketPriceResult(
                            market.Id,
                            market.Name,
                            price.Value,
                            searchUrl,
                            ProviderName,
                            IsLive: true,
                            ProductVarietyId: target.ProductVarietyId,
                            MatchedTitle: TryFindTitle(html) ?? target.DisplayName,
                            ImageUrl: TryFindImageUrl(html, market.BaseUrl),
                            ConfidenceScore: CalculateConfidence(html, target)));

                        break;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
                {
                    logger.LogWarning(ex, "{Market} icin canli fiyat cekilemedi.", market.Name);
                }
            }
        }

        return results;
    }

    private static IReadOnlyList<SearchTarget> BuildSearchTargets(Product product)
    {
        var varietyTargets = product.Varieties
            .Where(variety => variety.IsActive)
            .SelectMany(variety =>
                variety.SearchAliases
                    .OrderBy(alias => alias.Priority)
                    .Take(2)
                    .Select(alias => new SearchTarget(variety.Id, alias.Query, $"{variety.Name} {product.Name}", variety.Name, product.Name)))
            .ToList();

        if (varietyTargets.Count > 0)
        {
            return varietyTargets;
        }

        return [new SearchTarget(null, product.Name, product.Name, product.Name, product.Name)];
    }

    private static string BuildSearchUrl(Market market, string productName)
    {
        var encodedProduct = WebUtility.UrlEncode(productName);
        return string.Format(CultureInfo.InvariantCulture, market.SearchUrlTemplate!, encodedProduct);
    }

    private static decimal? TryFindPrice(string html)
    {
        foreach (Match match in JsonPriceRegex().Matches(html))
        {
            if (TryParsePrice(match.Groups["price"].Value, out var price))
            {
                return price;
            }
        }

        foreach (Match match in TurkishPriceRegex().Matches(html))
        {
            if (TryParsePrice(match.Groups["price"].Value, out var price))
            {
                return price;
            }
        }

        return null;
    }

    private static string? TryFindTitle(string html)
    {
        var match = TitleRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["title"].Value.Trim()) : null;
    }

    private static string? TryFindImageUrl(string html, string? baseUrl)
    {
        foreach (Match match in ImageRegex().Matches(html))
        {
            var imageUrl = WebUtility.HtmlDecode(match.Groups["url"].Value.Trim());
            if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.Contains("logo", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
            {
                return imageUrl;
            }

            if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(new Uri(baseUrl), imageUrl, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }
        }

        return null;
    }

    private static int CalculateConfidence(string html, SearchTarget target)
    {
        var normalized = html.ToLowerInvariant();
        var score = 40;

        if (normalized.Contains(target.ProductName.ToLowerInvariant()))
        {
            score += 25;
        }

        if (normalized.Contains(target.VarietyName.ToLowerInvariant()))
        {
            score += 25;
        }

        if (normalized.Contains("kg") || normalized.Contains("kilogram"))
        {
            score += 10;
        }

        return Math.Min(score, 100);
    }

    private static bool TryParsePrice(string value, out decimal price)
    {
        value = value.Trim().Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out price) && price > 0;
    }

    [GeneratedRegex("\"price\"\\s*:\\s*\"?(?<price>\\d{1,5}(?:[\\.,]\\d{1,2})?)\"?", RegexOptions.IgnoreCase)]
    private static partial Regex JsonPriceRegex();

    [GeneratedRegex("(?<price>\\d{1,5}(?:[\\.,]\\d{1,2})?)\\s*(?:TL|TRY)", RegexOptions.IgnoreCase)]
    private static partial Regex TurkishPriceRegex();

    [GeneratedRegex("<title[^>]*>(?<title>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("(?:src|data-src|content)=[\"'](?<url>https?://[^\"']+?\\.(?:jpg|jpeg|png|webp)(?:\\?[^\"']*)?)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex ImageRegex();

    private sealed record SearchTarget(int? ProductVarietyId, string Query, string DisplayName, string VarietyName, string ProductName);
}
