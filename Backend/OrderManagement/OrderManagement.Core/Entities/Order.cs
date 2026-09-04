using OrderManagement.Core.Enums;

namespace OrderManagement.Core.Entities;

public class Order
{
    public int Id { get; set; }

    // Shopify GraphQL ID, e.g. gid://shopify/Order/123456789
    // Null for WhatsApp/other non-Shopify orders.
    public string? ExternalOrderId { get; set; }

    // Human-friendly ID shown to staff, e.g. #1132 or WA05.
    public string DisplayOrderId { get; set; } = string.Empty;

    public int StoreId { get; set; }

    public OrderSource OrderSource { get; set; }

    public int CustomerId { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public string? TrackingNumber { get; set; }

    public string? LocationLink { get; set; }

    public string? FinalDecision { get; set; }

    public InvoiceStatus InvoiceStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "AED";

    public DateTime OrderDateUtc { get; set; }

    public int? WarehouseLocationId { get; set; }

    public DateTime LastStatusChangedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool NeedToShip { get; set; }

    public string? CancellationReturnReason { get; set; }

    public string? CancellationReturnEvidenceUrl { get; set; }

    public string? AirwayBillUrl { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Store Store { get; set; } = null!;

    public Customer Customer { get; set; } = null!;

    public WarehouseLocation? WarehouseLocation { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();

    public ICollection<OrderStatusHistory> StatusHistory { get; set; }
    = new List<OrderStatusHistory>();

    public ICollection<TrackingHistory> TrackingHistory { get; set; }
    = new List<TrackingHistory>();

    public ICollection<OrderNote> Notes { get; set; } = new List<OrderNote>();
    public ICollection<OrderNoteHistory> NoteHistory { get; set; } = new List<OrderNoteHistory>();
}
