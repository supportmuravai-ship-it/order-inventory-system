namespace OrderManagement.Core.DTOs.Tickets;

public class CreateTicketRequest
{
    public string AssignedToUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}