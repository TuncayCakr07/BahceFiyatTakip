using Microsoft.Playwright;

namespace BahceFiyatTakip.Services.MarketPrices;

public sealed class PlaywrightPageFetcher : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _semaphore = new(2);
    private readonly ILogger<PlaywrightPageFetcher> _logger;
    private bool _initialized;

    public PlaywrightPageFetcher(ILogger<PlaywrightPageFetcher> logger)
    {
        _logger = logger;
    }

    public async Task InitAsync()
    {
        try
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
            });
            _initialized = true;
            _logger.LogInformation("Playwright Chromium baslatildi.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Playwright baslatılamadı. Chromium kurulu olmayabilir. Sadece HttpClient kullanılacak.");
            _initialized = false;
        }
    }

    public bool IsAvailable => _initialized && _browser is not null;

    public async Task<string?> FetchAsync(string url, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            return null;
        }

        await _semaphore.WaitAsync(ct);

        IBrowserContext? context = null;

        try
        {
            context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
                Locale = "tr-TR",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7"
                }
            });

            var page = await context.NewPageAsync();

            await page.GotoAsync(url, new PageGotoOptions
            {
                Timeout = 6_000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            return await page.ContentAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Playwright sayfa getirilemedi: {Url}", url);
            return null;
        }
        finally
        {
            if (context is not null)
            {
                await context.CloseAsync();
            }

            _semaphore.Release();
        }
    }

    public async Task<(string? Html, IReadOnlyList<string> Links)> FetchWithLinksAsync(
        string url, string cssSelector, CancellationToken ct = default)
    {
        if (!IsAvailable) return (null, []);

        await _semaphore.WaitAsync(ct);
        IBrowserContext? context = null;
        try
        {
            context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
                Locale = "tr-TR",
                ExtraHTTPHeaders = new Dictionary<string, string> { ["Accept-Language"] = "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7" }
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions { Timeout = 6_000, WaitUntil = WaitUntilState.DOMContentLoaded });

            var html  = await page.ContentAsync();
            var links = await page.EvaluateAsync<string[]>(
                $"Array.from(document.querySelectorAll('{cssSelector}')).map(a=>a.href).filter(h=>h&&h.startsWith('http'))");

            return (html, links ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Playwright FetchWithLinks başarısız: {Url}", url);
            return (null, []);
        }
        finally
        {
            if (context is not null) await context.CloseAsync();
            _semaphore.Release();
        }
    }

    // Ürün sayfaları için: JS'nin API çağrılarını tamamlamasını bekler (NetworkIdle)
    public async Task<string?> FetchProductPageAsync(string url, CancellationToken ct = default)
    {
        if (!IsAvailable) return null;
        await _semaphore.WaitAsync(ct);
        IBrowserContext? context = null;
        try
        {
            context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
                Locale = "tr-TR",
                ExtraHTTPHeaders = new Dictionary<string, string> { ["Accept-Language"] = "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7" }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions { Timeout = 12_000, WaitUntil = WaitUntilState.NetworkIdle });
            return await page.ContentAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Playwright FetchProductPage başarısız: {Url}", url);
            return null;
        }
        finally
        {
            if (context is not null) await context.CloseAsync();
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetLinksAsync(string url, string cssSelector, CancellationToken ct = default)
    {
        if (!IsAvailable) return [];

        await _semaphore.WaitAsync(ct);
        IBrowserContext? context = null;
        try
        {
            context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
                Locale = "tr-TR"
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions { Timeout = 8_000, WaitUntil = WaitUntilState.DOMContentLoaded });

            var links = await page.EvaluateAsync<string[]>(
                $"Array.from(document.querySelectorAll('{cssSelector}')).map(a=>a.href).filter(h=>h&&h.startsWith('http'))");

            return links ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Playwright link getirilemedi: {Url}", url);
            return [];
        }
        finally
        {
            if (context is not null) await context.CloseAsync();
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }
}
