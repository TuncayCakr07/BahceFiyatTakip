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
builder.Services.AddHttpClient<LiveMarketPriceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<BraveWebSearchPriceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHttpClient<MockMarketPriceProvider>();
builder.Services.AddScoped<IMarketPriceProvider, CompositeMarketPriceProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
