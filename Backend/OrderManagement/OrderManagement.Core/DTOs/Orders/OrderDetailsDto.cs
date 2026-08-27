using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Orders;

public class OrderDetailsDto
{
    public int Id { get; set; }

    public string DisplayOrderId { get; set; } = string.Empty;

    public string? ExternalOrderId { get; set; }

    public DateTime OrderDateUtc { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? TrackingNumber { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public string? LocationLink { get; set; }

    public string? FinalDecision { get; set; }

    public OrderSource OrderSource { get; set; }

    public InvoiceStatus InvoiceStatus { get; set; }

    public string? WarehouseName { get; set; }

    public DateTime LastStatusChangedAtUtc { get; set; }

    public List<OrderItemDto> Items { get; set; } = [];
}

// For Phase 2 this is enough. We are still not adding status history, tracking history, tags, notes, or attachments.