using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Orders;

public class UpdateOrderStatusRequest
{
    public OrderStatus OrderStatus { get; set; }

    public string? Reason { get; set; }

    public string? EvidenceUrl { get; set; }
}