namespace BahceFiyatTakip.Services.MarketPrices.PlatformDetection;

public record PlatformDetectionResult(
    int MarketId,
    string MarketName,
    string Url,
    MarketPlatform Platform,
    double Confidence,
    string Reason,
    DateTime DetectedAt);
