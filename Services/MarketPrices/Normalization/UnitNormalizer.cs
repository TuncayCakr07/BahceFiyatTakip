using System.Globalization;
using System.Text.RegularExpressions;

namespace BahceFiyatTakip.Services.MarketPrices.Normalization;

/// <summary>
/// Ürün adından birim bilgisini çıkarır ve ham fiyatı birim başına fiyata normalize eder.
/// Desteklenen formatlar: g/gr/gram, kg, N adet, N'lü/li/lu/lı, demet, paket.
/// </summary>
public static partial class UnitNormalizer
{
    public static UnitNormalizationResult Normalize(
        string productName, decimal rawPrice, string systemUnit)
    {
        if (rawPrice <= 0)
            return Unreliable("", rawPrice, "Fiyat sıfır veya negatif");

        // 1. Gram: "500 g", "500g", "250 gr", "1000 gram"
        var gMatch = GramRegex().Match(productName);
        if (gMatch.Success &&
            int.TryParse(gMatch.Groups["a"].Value, out var grams) && grams > 0)
        {
            var kgQty = grams / 1000m;
            var mult  = 1m / kgQty;
            return new UnitNormalizationResult(
                gMatch.Value, "kg", kgQty, mult,
                decimal.Round(rawPrice * mult, 2), true);
        }

        // 2. Kg: "1 kg", "1,5 kg", "1.5kg", "2 KG"
        var kgMatch = KgRegex().Match(productName);
        if (kgMatch.Success)
        {
            var amtStr = kgMatch.Groups["a"].Value.Replace(",", ".");
            if (decimal.TryParse(amtStr, NumberStyles.Number, CultureInfo.InvariantCulture,
                    out var kgQty) && kgQty > 0)
            {
                var mult = 1m / kgQty;
                return new UnitNormalizationResult(
                    kgMatch.Value, "kg", kgQty, mult,
                    decimal.Round(rawPrice * mult, 2), true);
            }
        }

        // 3. Türkçe çoklu paket: "3'lü", "4'li", "6'lı", "2'lu"
        var tuMatch = TurkishPackRegex().Match(productName);
        if (tuMatch.Success &&
            int.TryParse(tuMatch.Groups["a"].Value, out var packCount) && packCount > 0)
        {
            var mult = 1m / packCount;
            return new UnitNormalizationResult(
                tuMatch.Value, "adet", packCount, mult,
                decimal.Round(rawPrice * mult, 2), true);
        }

        // 4. Adet (sayılı): "2 adet", "3 adet", "1 adet"
        var adetNumMatch = AdetWithNumberRegex().Match(productName);
        if (adetNumMatch.Success &&
            int.TryParse(adetNumMatch.Groups["a"].Value, out var adetNum) && adetNum > 0)
        {
            var mult = 1m / adetNum;
            return new UnitNormalizationResult(
                adetNumMatch.Value, "adet", adetNum, mult,
                decimal.Round(rawPrice * mult, 2), true);
        }

        // 5. Adet (sayısız): "adet" tek başına
        if (StandaloneAdetRegex().IsMatch(productName))
            return new UnitNormalizationResult("adet", "adet", 1m, 1m, rawPrice, true);

        // 6. Demet: "demet"
        if (DemetRegex().IsMatch(productName))
            return new UnitNormalizationResult("demet", "demet", 1m, 1m, rawPrice, true);

        // 7. Paket (sayısız): içerik belirsiz
        if (PaketRegex().IsMatch(productName))
            return Unreliable("paket", rawPrice, "Paket içeriği belirsiz — birim tespit edilemedi");

        // 8. Fallback: sistemdeki ürün birimi
        var sys = systemUnit.Trim().ToLowerInvariant();
        if (sys is "kg" or "kilo" or "kilogram")
            return new UnitNormalizationResult("", "kg", 1m, 1m, rawPrice, true);
        if (sys == "adet")
            return new UnitNormalizationResult("", "adet", 1m, 1m, rawPrice, true);

        return Unreliable("", rawPrice, "Birim tespit edilemedi");
    }

    private static UnitNormalizationResult Unreliable(
        string rawText, decimal rawPrice, string reason) =>
        new(rawText, "bilinmiyor", 0m, 0m, rawPrice, false, reason);

    // ── Regex'ler ────────────────────────────────────────────────────────────

    // "500 g", "500g", "250 gr", "1000 gram"
    [GeneratedRegex(@"(?<a>\d+)\s*(?:gram|gr|g)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GramRegex();

    // "1 kg", "1,5 kg", "1.5kg", "2 KG"
    [GeneratedRegex(@"(?<a>\d+(?:[,\.]\d+)?)\s*kg\b", RegexOptions.IgnoreCase)]
    private static partial Regex KgRegex();

    // "3'lü", "4'li", "6'lı", "2'lu", "3'lük", "4'lik"
    // Hem ASCII apostrof (') hem Türkçe sağ tırnak (') desteklenir
    [GeneratedRegex("(?<a>\\d+)['’]l[\\u00fc\\u0075\\u0131\\u0069]k?\\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex TurkishPackRegex();

    // "2 adet", "3 adet", "1 adet"
    [GeneratedRegex(@"(?<a>\d+)\s*adet\b", RegexOptions.IgnoreCase)]
    private static partial Regex AdetWithNumberRegex();

    // "adet" — sayı olmadan
    [GeneratedRegex(@"\badet\b", RegexOptions.IgnoreCase)]
    private static partial Regex StandaloneAdetRegex();

    // "demet"
    [GeneratedRegex(@"\bdemet\b", RegexOptions.IgnoreCase)]
    private static partial Regex DemetRegex();

    // "paket" — sayısız (sayılı paket diğer kurallarca yakalanır)
    [GeneratedRegex(@"\bpaket\b", RegexOptions.IgnoreCase)]
    private static partial Regex PaketRegex();
}
