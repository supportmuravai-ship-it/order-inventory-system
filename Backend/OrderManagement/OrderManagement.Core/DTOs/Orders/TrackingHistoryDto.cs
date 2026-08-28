namespace OrderManagement.Core.DTOs.Orders;

public class TrackingHistoryDto
{
    public string? OldTrackingNumber { get; set; }

    public string? NewTrackingNumber { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public string ChangedBy { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }
}