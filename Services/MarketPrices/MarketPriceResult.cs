namespace BahceFiyatTakip.Services.MarketPrices;

public record MarketPriceResult(
    int MarketId,
    string MarketName,
    decimal PricePerKg,
    string? SourceUrl,
    string ProviderName,
    bool IsLive = false,
    int? ProductVarietyId = null,
    string? MatchedTitle = null,
    string? ImageUrl = null,
    int ConfidenceScore = 0,
    bool? InStock = null);
