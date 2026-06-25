namespace BahceFiyatTakip.Models;

public class ProductMarketLink
{
    public int Id { get; set; }
    public int ProductVarietyId { get; set; }
    public int MarketId { get; set; }
    public string DirectUrl { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public ProductVariety ProductVariety { get; set; } = null!;
    public Market Market { get; set; } = null!;
}
