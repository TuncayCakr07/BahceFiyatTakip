namespace BahceFiyatTakip.Services.MarketPrices.Validation;

public record ValidationResult(
    bool            IsValid,
    ValidationRule? FailedRule,
    string?         RejectReason,
    int             ConfidenceScore = 0)
{
    public static ValidationResult Ok(int confidence) =>
        new(true, null, null, confidence);

    public static ValidationResult Reject(ValidationRule rule, string reason, int confidence = 0) =>
        new(false, rule, reason, confidence);
}
