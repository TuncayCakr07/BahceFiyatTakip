# BahceFiyatTakip

ASP.NET Core MVC, .NET 8, Entity Framework Core ve SQL Server ile hazirlanan bahce urunleri market fiyat takip MVP uygulamasi.

## Ozellikler

- Urun ekleme ve duzenleme
- Migros, A101, CarrefourSA, Sok ve BIM icin market kayitlari
- Canli HTML fiyat cekme denemesi
- Brave Search API anahtari verilirse genel web/e-ticaret aramasi
- Canli veri alinamayan marketlerde mock fallback
- Fiyatlari SQL Server veritabanina kaydetme
- Urun cesidi, eslesen baslik, resim URL'si ve guven skoru kaydetme
- Gunluk/aylik gecmis icin tarih, urun ve market filtreleme
- LocalDb, SQL Server Express veya production SQL Server ile calisma

## Connection string onceligi

Uygulama connection string'i once ortam degiskeninden okur:

```powershell
$env:BAHCEFIYATTAKIP_CONNECTIONSTRING="Server=.;Database=BahceFiyatTakipDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Ortam degiskeni yoksa `appsettings.{Environment}.json` veya `appsettings.json` icindeki `ConnectionStrings:DefaultConnection` kullanilir.

Development varsayilani:

```text
Server=(localdb)\MSSQLLocalDB;Database=BahceFiyatTakipDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Production ornegi:

```text
Server=CANLI_SERVER;Database=BahceFiyatTakipDb;User Id=DB_USER;Password=DB_PASSWORD;TrustServerCertificate=True
```

## Localde calistirma

```powershell
dotnet restore
dotnet ef database update
dotnet run
```

Tarayicida `https://localhost:xxxx` veya `http://localhost:xxxx` adresini acin.

## Migration komutlari

Ilk migration projede hazir:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Yeni tablo/alan eklenirse:

```powershell
dotnet ef migrations add MigrationAdi
dotnet ef database update
```

Canli sunucuda migration calistirmadan once production connection string'i dogru ayarlayin.

## Publish alma

```powershell
dotnet publish -c Release -o .\publish
```

## IIS'e yayinlama

1. Sunucuda .NET 8 Hosting Bundle kurulu olsun.
2. `dotnet publish -c Release -o .\publish` ciktisini IIS site klasorune kopyalayin.
3. IIS Application Pool icin `No Managed Code` secin.
4. Production connection string'i ortam degiskeni olarak tanimlayin:

```powershell
[Environment]::SetEnvironmentVariable("BAHCEFIYATTAKIP_CONNECTIONSTRING", "Server=CANLI_SERVER;Database=BahceFiyatTakipDb;User Id=DB_USER;Password=DB_PASSWORD;TrustServerCertificate=True", "Machine")
```

5. `dotnet ef database update` komutunu production connection string ile calistirin.

## Hosting/panele yayinlama

1. Panel .NET 8 ASP.NET Core desteklemeli.
2. Publish klasorunu paneldeki uygulama dizinine yukleyin.
3. Panelden `ASPNETCORE_ENVIRONMENT=Production` ayarlayin.
4. Panelin ortam degiskeni bolumunden `BAHCEFIYATTAKIP_CONNECTIONSTRING` degerini canli SQL Server bilgileriyle girin.
5. Migration calistirma destegi yoksa migration SQL script'i alin:

```powershell
dotnet ef migrations script -o migration.sql
```

Bu script'i hosting SQL panelinden veritabanina uygulayin.

## Canli fiyat altyapisi

`LiveMarketPriceProvider` market arama sayfalarini `HttpClient` ile okur ve HTML icindeki fiyat, baslik ve resim desenlerinden ilk uygun sonucu yakalamaya calisir. Market sitesi engellerse, sayfa yapisi degisirse veya fiyat bulunamazsa `CompositeMarketPriceProvider` ilgili market icin `MockMarketPriceProvider` sonucunu kaydeder. Sonuc ekraninda kaynak `Canli` veya `Mock` olarak gorunur.

`BraveWebSearchPriceProvider`, `BRAVE_SEARCH_API_KEY` ortam degiskeni veya `SearchApis:Brave:ApiKey` ayari varsa zincir market disindaki web sitelerinde de arama yapar. Bu katman Okitsu, Orri, Murcott, Mayer, Finger Lime gibi nis cesitler icin devreye girer.

```powershell
$env:BRAVE_SEARCH_API_KEY="BRAVE_API_KEYINIZ"
```

## Seed edilen urun cesitleri

Mandalina, limon, lime, finger lime, portakal, avokado ve diger narenciye urunleri icin 35 cesit ve arama alias'lari seed edildi. Ornekler: OKITSU, SATSUMA, KLEMANTIN, ORRI, MURCOTT, MAYER, ENTERDONAT, LAMAS, VERDE, ROSE, WASHINGTON, VALENSIYA, HASS, ETTINGER, BERGAMOT, KUMKUAT, GREYFURT.
