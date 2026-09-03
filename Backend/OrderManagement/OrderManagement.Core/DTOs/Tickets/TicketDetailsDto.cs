using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Tickets;

public class TicketDetailsDto
{
    public int Id { get; set; }

    public int? OrderId { get; set; }
    public string? DisplayOrderId { get; set; }

    public string AssignedToUserId { get; set; } = string.Empty;
    public string AssignedToEmail { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;

    public string? ClosedByUserId { get; set; }
    public string? ClosedByEmail { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public TicketStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}