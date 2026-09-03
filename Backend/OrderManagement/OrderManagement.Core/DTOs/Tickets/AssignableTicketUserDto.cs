namespace OrderManagement.Core.DTOs.Tickets;

public class AssignableTicketUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}