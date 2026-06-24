using BahceFiyatTakip.Data;
using BahceFiyatTakip.Services;
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

builder.Services.AddSingleton<PlaywrightPageFetcher>();

builder.Services.AddHttpClient<LiveMarketPriceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<BraveWebSearchPriceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddScoped<IMarketPriceProvider, CompositeMarketPriceProvider>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await MarketCatalogSeeder.EnsureSeededAsync(dbContext);
}

// Playwright browser'ı başlat (Chromium kuruluysa)
var playwrightFetcher = app.Services.GetRequiredService<PlaywrightPageFetcher>();
await playwrightFetcher.InitAsync();

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




