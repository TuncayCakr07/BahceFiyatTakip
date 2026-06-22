using BahceFiyatTakip.Models;

namespace BahceFiyatTakip.ViewModels;

public class PriceHistoryViewModel
{
    public int? ProductId { get; set; }

    public int? MarketId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public IReadOnlyList<Product> Products { get; set; } = [];

    public IReadOnlyList<Market> Markets { get; set; } = [];

    public IReadOnlyList<PriceRecord> Records { get; set; } = [];
}
