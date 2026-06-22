using System.ComponentModel.DataAnnotations;

namespace BahceFiyatTakip.Models;

public class ProductSearchAlias
{
    public int Id { get; set; }

    public int ProductVarietyId { get; set; }

    public ProductVariety ProductVariety { get; set; } = null!;

    [Required]
    [StringLength(160)]
    public string Query { get; set; } = string.Empty;

    public int Priority { get; set; } = 100;
}
