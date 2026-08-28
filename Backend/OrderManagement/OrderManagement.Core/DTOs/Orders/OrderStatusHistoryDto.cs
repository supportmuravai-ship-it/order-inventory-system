using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Orders;

public class OrderStatusHistoryDto
{
    public OrderStatus OldStatus { get; set; }

    public OrderStatus NewStatus { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public string ChangedBy { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }
}