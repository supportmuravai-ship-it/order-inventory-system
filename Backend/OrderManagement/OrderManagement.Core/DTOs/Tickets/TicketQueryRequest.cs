using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Tickets;

public class TicketQueryRequest
{
    public TicketStatus? Status { get; set; }

    public string? AssignedToUserId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public string? Search { get; set; }
}