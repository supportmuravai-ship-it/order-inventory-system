using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Orders;

public class OrderQueryRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public OrderStatus? OrderStatus { get; set; }

    public string? Product { get; set; }

    public string? SKU { get; set; }

    public OrderSource? OrderSource { get; set; }

    public InvoiceStatus? InvoiceStatus { get; set; }

    public string Sort { get; set; } = "newest";
}