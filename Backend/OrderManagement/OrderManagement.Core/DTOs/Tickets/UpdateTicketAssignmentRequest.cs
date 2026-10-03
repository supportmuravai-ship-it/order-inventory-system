namespace OrderManagement.Core.DTOs.Tickets;

public class UpdateTicketAssignmentRequest
{
    public List<string> AssignedToUserIds { get; set; } = [];
}