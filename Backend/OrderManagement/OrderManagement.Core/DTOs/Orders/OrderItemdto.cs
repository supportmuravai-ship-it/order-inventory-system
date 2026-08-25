namespace OrderManagement.Core.DTOs.Orders;

public class OrderItemDto
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? VariantName { get; set; }

    public string? SKU { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}