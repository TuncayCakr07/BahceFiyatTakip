using System.ComponentModel.DataAnnotations;

namespace BahceFiyatTakip.Models;

public class ProductVariety
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? HarvestPeriod { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ProductSearchAlias> SearchAliases { get; set; } = new List<ProductSearchAlias>();

    public ICollection<PriceRecord> PriceRecords { get; set; } = new List<PriceRecord>();
}
