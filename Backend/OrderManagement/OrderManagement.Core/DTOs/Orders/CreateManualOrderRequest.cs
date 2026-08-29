using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Orders;

public class CreateManualOrderRequest
{
    public string DisplayOrderId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public DateTime OrderDateUtc { get; set; }

    public OrderSource OrderSource { get; set; }

    public decimal TotalAmount { get; set; }

    public List<CreateManualOrderItemRequest> Items { get; set; } = [];
}

public class CreateManualOrderItemRequest
{
    public string ProductName { get; set; } = string.Empty;

    public string? VariantName { get; set; }

    public string? SKU { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}