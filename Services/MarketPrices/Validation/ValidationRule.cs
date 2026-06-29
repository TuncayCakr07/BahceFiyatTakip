namespace BahceFiyatTakip.Services.MarketPrices.Validation;

public enum ValidationRule
{
    PriceAboveZero,
    InStockNotFalse,
    ProductNameMatch,
    ConfidenceThreshold
}
