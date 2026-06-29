using BahceFiyatTakip.Data;
using BahceFiyatTakip.Services;
using BahceFiyatTakip.Services.ExcelSeed;
using BahceFiyatTakip.Services.MarketPrices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    Environment.GetEnvironmentVariable("BAHCEFIYATTAKIP_CONNECTIONSTRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection bulunamadi. appsettings veya BAHCEFIYATTAKIP_CONNECTIONSTRING ayarlayin.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IPriceTrackingService, PriceTrackingService>();
builder.Services.AddScoped<MarketHealthService>();
builder.Services.AddScoped<DirectUrlDiscoveryService>();

builder.Services.AddHttpClient<LiveMarketPriceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<BraveWebSearchPriceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<BingWebSearchPriceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<PlaywrightPageFetcher>();
builder.Services.AddSingleton<BahceFiyatTakip.Services.MarketPrices.Adapters.MacrocenterPriceAdapter>();
builder.Services.AddHttpClient<BahceFiyatTakip.Services.MarketPrices.Adapters.MigrosPriceAdapter>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddScoped<IMarketPriceProvider, CompositeMarketPriceProvider>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await MarketCatalogSeeder.EnsureSeededAsync(dbContext);
    await DirectUrlSeed.EnsureSeededAsync(dbContext);

    // Excel seed: sadece hiç Excel link'i yoksa çalış (DirectUrl="" olan link yoksa)
    bool excelAlreadySeeded = await dbContext.ProductMarketLinks.AnyAsync(l => l.DirectUrl == "");
    if (!excelAlreadySeeded)
    {
        const string excelPath = @"C:\Users\Tuncay Çakır\Desktop\Yeni Fiyat Araştırması.xlsx";
        if (File.Exists(excelPath))
        {
            var seeder = new ExcelSeedService(dbContext, app.Services.GetRequiredService<ILogger<ExcelSeedService>>());
            var result = await seeder.SeedAsync(excelPath);
            app.Logger.LogInformation(
                "Excel seed tamamlandı → +{P} ürün, +{V} çeşit, +{M} market, +{L} link, +{A} alias",
                result.Products, result.Varieties, result.Markets, result.Links, result.Aliases);
        }
        else
        {
            app.Logger.LogWarning("Excel dosyası bulunamadı: {Path}", excelPath);
        }
    }
}

var pageFetcher = app.Services.GetRequiredService<PlaywrightPageFetcher>();
await pageFetcher.InitAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();




