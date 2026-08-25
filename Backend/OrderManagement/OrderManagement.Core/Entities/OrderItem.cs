namespace OrderManagement.Core.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string? ExternalLineItemId { get; set; }

    public string? ExternalProductId { get; set; }

    public string? ExternalVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? VariantName { get; set; }

    public string? SKU { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}