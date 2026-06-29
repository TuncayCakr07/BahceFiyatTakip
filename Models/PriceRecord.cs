using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BahceFiyatTakip.Models;

public class PriceRecord
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int? ProductVarietyId { get; set; }

    public ProductVariety? ProductVariety { get; set; }

    public int MarketId { get; set; }

    public Market Market { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Kg Fiyati")]
    public decimal PricePerKg { get; set; }

    [Display(Name = "Kontrol Tarihi")]
    public DateTime CheckedAt { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? SourceUrl { get; set; }

    [StringLength(300)]
    public string? ImageUrl { get; set; }

    [StringLength(200)]
    public string? MatchedTitle { get; set; }

    [StringLength(100)]
    public string SourceProvider { get; set; } = "Mock";

    public bool IsLive { get; set; }

    public bool? InStock { get; set; }

    public int ConfidenceScore { get; set; }
}
