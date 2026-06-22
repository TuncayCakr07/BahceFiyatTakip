using System.ComponentModel.DataAnnotations;

namespace BahceFiyatTakip.Models;

public class Market
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? BaseUrl { get; set; }

    [StringLength(300)]
    public string? SearchUrlTemplate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PriceRecord> PriceRecords { get; set; } = new List<PriceRecord>();
}
