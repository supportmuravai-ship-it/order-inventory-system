using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Orders;

public class UpdateOrderStatusRequest
{
    public OrderStatus OrderStatus { get; set; }
}