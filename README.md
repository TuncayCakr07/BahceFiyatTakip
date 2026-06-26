# Bahçe Fiyat Takip

ASP.NET Core MVC · .NET 8 · Entity Framework Core · SQL Server LocalDB

Narenciye, tropikal meyve, sebze ve işlenmiş ürünlerin market fiyatlarını takip eden kişisel web uygulaması.

---

## Özellikler

- **220+ doğrudan URL** ile 50+ marketten otomatik fiyat çekme (Playwright/Chromium)
- **67 ürün çeşidi** — mandalina, limon, lime, portakal, avokado, nar, ejder meyvesi, reçel, nar ekşisi, kurutulmuş ürünler, meyve suyu ve daha fazlası
- Animasyonlu sol sidebar ile **kategori grupları** (Narenciye, Tropikal, Meyve, Sebze, İşlenmiş)
- **Fiyatı Düşen Ürünler** şeridi — 7 günden önceki fiyatla otomatik karşılaştırma
- Kart üzerinde trend rozetleri (↓ düştü / ↑ yükseldi)
- Fiyat geçmişi ve market karşılaştırması
- Migration + seed verisi otomatik çalışır — kurulum sonrası hazır

---

## Localde Kurulum

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (Visual Studio ile gelir veya [ayrı indirilebilir](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb))

### İlk Kurulum

```powershell
git clone https://github.com/TuncayCakr07/BahceFiyatTakip.git
cd BahceFiyatTakip
dotnet build
pwsh bin\Debug\net8.0\playwright.ps1 install chromium
dotnet run
```

Tarayıcıda `http://localhost:5049` adresini açın.

> **Not:** `playwright install chromium` yalnızca bir kez çalıştırılır. Chromium (~150 MB) indirilir ve yerel cache'e kaydedilir.

### Sonraki Başlatmalar

```powershell
dotnet run
```

Migration ve seed verileri her başlatmada otomatik kontrol edilir.

---

## Connection String

Varsayılan (LocalDB):

```
Server=(localdb)\MSSQLLocalDB;Database=BahceFiyatTakipDb;Trusted_Connection=True;TrustServerCertificate=True
```

Farklı bir SQL Server için ortam değişkeni tanımlayın:

```powershell
$env:BAHCEFIYATTAKIP_CONNECTIONSTRING="Server=.;Database=BahceFiyatTakipDb;Trusted_Connection=True;TrustServerCertificate=True"
```

---

## Migration Komutları

```powershell
# Yeni migration oluştur
dotnet ef migrations add MigrationAdi

# Veritabanına uygula
dotnet ef database update

# Migration SQL scripti al (hosting paneli için)
dotnet ef migrations script -o migration.sql
```

---

## Fiyat Çekme Altyapısı

`DirectUrlSeed.cs` — her ürün çeşidi için market URL'leri seed edilir (startup'ta `ProductMarketLinks` tablosuna işlenir).

`PlaywrightPageFetcher` — Chromium headless tarayıcısıyla sayfayı açar, fiyat/başlık/resim bilgisini çeker.

"Fiyat Güncelle" butonuna tıklandığında tüm seed URL'leri ziyaret edilerek güncel fiyatlar veritabanına kaydedilir.

---

## Seed Edilen Ürünler (67 Çeşit)

| Kategori    | Ürünler                                                                       |
|-------------|-------------------------------------------------------------------------------|
| Narenciye   | Mandalina (5 çeşit), Limon (7), Lime (7), Portakal (5), Greyfurt (2), Bergamot, Kumkuat, Turunç, Şadok, Limon Otu |
| Tropikal    | Avokado (5 çeşit), Ejder Meyvesi (2), Çarkıfelek (2), Mango (3)              |
| Meyve       | Nar (2 çeşit)                                                                 |
| Sebze       | Domates (3 çeşit)                                                             |
| İşlenmiş    | Reçel & Marmelat (8), Nar Ekşisi, Kurutulmuş Ürünler (5), Meyve Suyu (2)     |

---

## Publish / Deploy

```powershell
dotnet publish -c Release -o .\publish
```

IIS'e taşımak için `publish` klasörünü site dizinine kopyalayın, Application Pool'u `No Managed Code` olarak ayarlayın ve connection string'i ortam değişkeni olarak tanımlayın.
