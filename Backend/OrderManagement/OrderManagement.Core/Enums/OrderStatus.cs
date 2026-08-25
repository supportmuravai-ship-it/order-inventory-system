namespace OrderManagement.Core.Enums;

public enum OrderStatus
{
    Confirmed,
    Shipped,
    Delivered,
    NoResponse,
    Return,
    ReturnInProcess,
    Cancelled,
    RepeatedOrder
}