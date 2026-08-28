using OrderManagement.Core.Enums;

namespace OrderManagement.Core.Entities;

public class OrderStatusHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public OrderStatus OldStatus { get; set; }

    public OrderStatus NewStatus { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}