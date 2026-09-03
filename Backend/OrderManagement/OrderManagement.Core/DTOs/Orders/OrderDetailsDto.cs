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

    public bool NeedsAttention { get; set; }

    public double HoursInCurrentStatus { get; set; }

    public string? ShoaibNote { get; set; }

    public DateTime? ShoaibNoteUpdatedAtUtc { get; set; }

    public string? ShoaibNoteUpdatedBy { get; set; }

    public string? TrenvoNote { get; set; }

    public DateTime? TrenvoNoteUpdatedAtUtc { get; set; }

    public string? TrenvoNoteUpdatedBy { get; set; }

    public bool NeedToShip { get; set; }

    public string? AirwayBillUrl { get; set; }
    public string? CancellationReturnReason { get; set; }

    public string? CancellationReturnEvidenceUrl { get; set; }

    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = [];

    public List<TrackingHistoryDto> TrackingHistory { get; set; } = [];

}

