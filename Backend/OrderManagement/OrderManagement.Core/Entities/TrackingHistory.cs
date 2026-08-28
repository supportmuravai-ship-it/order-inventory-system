namespace OrderManagement.Core.Entities;

public class TrackingHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string? OldTrackingNumber { get; set; }

    public string? NewTrackingNumber { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}