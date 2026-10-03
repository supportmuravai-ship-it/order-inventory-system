namespace OrderManagement.Core.DTOs.Tickets;

public class CreateTicketRequest
{
    public List<string> AssignedToUserIds { get; set; } = [];
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}