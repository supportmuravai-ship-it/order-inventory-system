namespace OrderManagement.Core.DTOs.Tickets;

public class CreateTicketFromPageRequest
{
    public string AssignedToUserId { get; set; } = string.Empty;

    public string? DisplayOrderId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}