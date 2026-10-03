namespace OrderManagement.Core.Entities;

public class OrderTicketAssignee
{
    public int OrderTicketId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public OrderTicket OrderTicket { get; set; } = null!;
}