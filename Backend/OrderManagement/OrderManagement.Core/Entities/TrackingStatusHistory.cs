namespace OrderManagement.Core.Entities;

public class TrackingStatusHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}