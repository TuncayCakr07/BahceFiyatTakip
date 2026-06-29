using System.Globalization;

namespace BahceFiyatTakip.Services.MarketPrices.Validation;

/// <summary>
/// Adapter'lardan gelen ham çıktının doğrulanması ve güven skoru hesabı.
/// Tüm market adapter'ları bu sınıfı ortak kullanır.
/// </summary>
public sealed record CandidateInput(
    string  MatchedTitle,
    decimal NormalizedPrice,
    string  UnitLabel,         // "kg" | "adet" | "bilinmiyor"
    bool?   InStock,
    string  ExpectedProduct,
    string  ExpectedVariety);

public static class ValidationGate
{
    public const int MinConfidence = 40;

    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static ValidationResult Validate(CandidateInput input)
    {
        // Kural 1: Fiyat > 0
        if (input.NormalizedPrice <= 0)
            return ValidationResult.Reject(
                ValidationRule.PriceAboveZero,
                "Fiyat sıfır veya negatif");

        // Kural 2: Stok yok → ret
        if (input.InStock == false)
            return ValidationResult.Reject(
                ValidationRule.InStockNotFalse,
                "Stok yok");

        // Kural 3: Ürün adı eşleşmesi — skor hesabından önce hard check
        var hay  = N(input.MatchedTitle);
        var prod = N(input.ExpectedProduct);

        if (!string.IsNullOrEmpty(prod) && !ContainsTurkishWord(hay, prod))
            return ValidationResult.Reject(
                ValidationRule.ProductNameMatch,
                $"Ürün adı eşleşmedi: '{input.MatchedTitle}' ← beklenen '{input.ExpectedProduct}'");

        // Güven skoru hesapla
        int confidence = ComputeConfidence(
            hay, prod, input.ExpectedVariety,
            input.UnitLabel, input.InStock);

        // Kural 4: Güven eşiği
        if (confidence < MinConfidence)
            return ValidationResult.Reject(
                ValidationRule.ConfidenceThreshold,
                $"Güven skoru düşük: {confidence} < {MinConfidence}",
                confidence);

        return ValidationResult.Ok(confidence);
    }

    // ── Güven skoru hesabı ───────────────────────────────────────────────────

    private static int ComputeConfidence(
        string normHay, string normProd, string varietyName,
        string unitLabel, bool? inStock)
    {
        int score = 50; // temel: kaynak bulundu ve veri çıkarıldı

        // +20 ürün adı (Kural 3 tarafından zaten doğrulandı)
        if (!string.IsNullOrEmpty(normProd) && ContainsTurkishWord(normHay, normProd))
            score += 20;

        // +10 çeşit
        var var_ = N(varietyName);
        if (!string.IsNullOrEmpty(var_) && var_ != normProd && ContainsTurkishWord(normHay, var_))
            score += 10;

        // ±birim netliği
        score += unitLabel switch
        {
            "kg" or "adet" =>  10,
            "bilinmiyor"   => -15,
            _              =>   0,
        };

        // ±stok bilgisi
        score += inStock switch
        {
            true  =>  10,
            null  =>  -5,
            false =>   0,
        };

        return Math.Clamp(score, 0, 100);
    }

    // ── Türkçe yardımcılar (public — adapter'lar kullanabilir) ───────────────

    public static string N(string s) =>
        s.ToLower(TrCulture)
         .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
         .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");

    public static bool ContainsTurkishWord(string hay, string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        if (word.Length < 4) return hay.Contains(word, StringComparison.Ordinal);

        var idx = hay.IndexOf(word, StringComparison.Ordinal);
        while (idx >= 0)
        {
            bool startOk = idx == 0 || !char.IsLetter(hay[idx - 1]);
            if (startOk)
            {
                int after = idx + word.Length;
                if (after >= hay.Length || !char.IsLetter(hay[after])) return true;
                int suf = 0;
                while (after + suf < hay.Length && char.IsLetter(hay[after + suf]) && suf < 5) suf++;
                bool endOk = after + suf >= hay.Length || !char.IsLetter(hay[after + suf]);
                if (endOk && suf is >= 1 and <= 5) return true;
            }
            idx = hay.IndexOf(word, idx + 1, StringComparison.Ordinal);
        }
        return false;
    }
}
