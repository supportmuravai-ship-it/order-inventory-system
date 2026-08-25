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

    public byte[] RowVersion { get; set; } = [];

    public Store Store { get; set; } = null!;

    public Customer Customer { get; set; } = null!;

    public WarehouseLocation? WarehouseLocation { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();
}

//what is deliberately absent:

//AirwayBill
//Tag string
//ShoaibNote string
//TrenvoNote string
//StatusHistory
//TrackingHistory
//Attachments
//Courier
//Inventory quantities

//That is intentional.Your Phase 2 brief says those future systems must not be prematurely represented with bad temporary schema.