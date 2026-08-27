using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Orders;

public class UpdateInvoiceStatusRequest
{
    public InvoiceStatus InvoiceStatus { get; set; }
}