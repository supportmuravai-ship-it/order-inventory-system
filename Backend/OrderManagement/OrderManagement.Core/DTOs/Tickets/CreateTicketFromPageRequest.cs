namespace OrderManagement.Core.DTOs.Tickets;

public class CreateTicketFromPageRequest
{
    public List<string> AssignedToUserIds { get; set; } = [];

    public string? DisplayOrderId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}