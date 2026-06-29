using System.Net;
using System.Text.RegularExpressions;
using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.Services.MarketPrices.PlatformDetection;

public class MarketPlatformDetector(
    HttpClient httpClient,
    ILogger<MarketPlatformDetector> logger) : IMarketPlatformDetector
{
    public async Task<PlatformDetectionResult> DetectAsync(Market market, CancellationToken cancellationToken = default)
    {
        try
        {
            // Kısa timeout ile sayfayı çek
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var url = market.BaseUrl ?? "http://unknown";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            
            using var resp = await httpClient.SendAsync(req, cts.Token);
            if (!resp.IsSuccessStatusCode) 
                return Unknown(market, url, $"HTTP {(int)resp.StatusCode}");

            var html = await resp.Content.ReadAsStringAsync(cts.Token);

            // 1. Özel Market Tanımları (Domain/Ad bazlı)
            if (market.Name.Contains("Macrocenter", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.Macrocenter, 1.0, "Exact market name match");
            if (market.Name.Contains("Migros", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.Migros, 1.0, "Exact market name match");
            if (market.Name.Contains("CarrefourSA", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.CarrefourSA, 1.0, "Exact market name match");

            // 2. Next.js İşareti
            if (html.Contains("__NEXT_DATA__", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.GenericNextJs, 0.9, "Found __NEXT_DATA__ script block");

            // 3. JSON-LD Product İşareti
            if (html.Contains("application/ld+json", StringComparison.OrdinalIgnoreCase) && 
                html.Contains("\"@type\": \"Product\"", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.GenericJsonLd, 0.8, "Found JSON-LD Product schema");

            // 4. SapCommerce / Hybris
            if (html.Contains("hybris", StringComparison.OrdinalIgnoreCase) || 
                html.Contains("/rest/v2/", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.SapCommerce, 0.7, "Found Hybris/SAP Commerce markers");

            // 5. Ticimax
            if (html.Contains("productDetailModel", StringComparison.OrdinalIgnoreCase) || 
                html.Contains("ticimax", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.Ticimax, 0.8, "Found Ticimax markers");

            // 6. WooCommerce
            if (html.Contains("wp-content", StringComparison.OrdinalIgnoreCase) || 
                html.Contains("woocommerce", StringComparison.OrdinalIgnoreCase) || 
                html.Contains("wc-ajax", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.WooCommerce, 0.8, "Found WooCommerce/WordPress markers");

            // 7. IdeaSoft
            if (html.Contains("ideasoft", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.IdeaSoft, 0.7, "Found IdeaSoft markers");

            // 8. NopCommerce
            if (html.Contains("nopCommerce", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.NopCommerce, 0.7, "Found NopCommerce markers");

            // 9. GraphQL / REST API İpuçları
            if (html.Contains("graphql", StringComparison.OrdinalIgnoreCase))
                return Result(market, url, MarketPlatform.GraphQL, 0.6, "Found GraphQL reference");
            
            if (html.Contains("application/json", StringComparison.OrdinalIgnoreCase) && 
                (html.StartsWith("{") || html.StartsWith("[")))
                return Result(market, url, MarketPlatform.RestApi, 0.6, "Response is raw JSON");

            return Unknown(market, url, "No identifying markers found");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Platform detection failed for {Market}: {Msg}", market.Name, ex.Message);
            return Unknown(market, market.BaseUrl ?? "http://unknown", $"Error: {ex.Message}");
        }
    }

    private static PlatformDetectionResult Result(Market m, string url, MarketPlatform p, double conf, string reason)
        => new(m.Id, m.Name, url, p, conf, reason, DateTime.UtcNow);

    private static PlatformDetectionResult Unknown(Market m, string url, string reason)
        => new(m.Id, m.Name, url, MarketPlatform.Unknown, 0.0, reason, DateTime.UtcNow);
}
