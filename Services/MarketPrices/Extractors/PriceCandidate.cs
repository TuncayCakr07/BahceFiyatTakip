namespace BahceFiyatTakip.Services.MarketPrices.Extractors;

public sealed record PriceCandidate(
    string  Title,
    decimal Price,
    string  Url,
    string? ImageUrl,
    bool    InStock);
