namespace BahceFiyatTakip.Services.MarketPrices.Normalization;

/// <summary>
/// Ürün adından çıkarılan birim bilgisi ve normalize edilmiş fiyat.
/// </summary>
public record UnitNormalizationResult(
    string  RawUnitText,      // Ürün adında bulunan ham birim metni ("500 g", "3'lü", vb.)
    string  NormalizedUnit,   // Standart birim: "kg" | "adet" | "demet" | "bilinmiyor"
    decimal Quantity,         // Hedef birim cinsinden miktar (500g → 0.5, 3'lü → 3)
    decimal Multiplier,       // Fiyat çarpanı = 1/Quantity (birim başına fiyata dönüşüm)
    decimal NormalizedPrice,  // rawPrice * Multiplier → birim başına fiyat
    bool    IsReliable,       // Birim güvenilir şekilde tespit edilebildi mi?
    string? RejectReason = null);
