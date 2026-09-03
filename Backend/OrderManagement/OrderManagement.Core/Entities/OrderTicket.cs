using OrderManagement.Core.Enums;

namespace OrderManagement.Core.Entities;

public class OrderTicket
{
    public int Id { get; set; }

    public int StoreId { get; set; }
    public int? OrderId { get; set; }

    public string AssignedToUserId { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedByUserId { get; set; }

    public Store Store { get; set; } = null!;
    public Order? Order { get; set; }
}