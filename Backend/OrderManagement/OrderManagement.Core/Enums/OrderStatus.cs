namespace OrderManagement.Core.Enums;

public enum OrderStatus
{
    Confirmed = 0,
    Shipped = 1,
    Delivered = 2,
    NoResponse = 3,
    Return = 4,
    ReturnInProcess = 5,
    Cancelled = 6,
    RepeatedOrder = 7,
    New = 8
}