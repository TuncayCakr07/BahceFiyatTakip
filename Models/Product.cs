using System.ComponentModel.DataAnnotations;

namespace BahceFiyatTakip.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Urun adi zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Urun Adi")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Kategori")]
    public string Category { get; set; } = "Narenciye";

    [StringLength(20)]
    [Display(Name = "Birim")]
    public string Unit { get; set; } = "Kg";

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    [Display(Name = "Varsayılan Resim URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Kayit Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PriceRecord> PriceRecords { get; set; } = new List<PriceRecord>();

    public ICollection<ProductVariety> Varieties { get; set; } = new List<ProductVariety>();
}
