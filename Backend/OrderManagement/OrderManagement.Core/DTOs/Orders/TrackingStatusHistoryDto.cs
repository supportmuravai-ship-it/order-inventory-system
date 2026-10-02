namespace OrderManagement.Core.DTOs.Orders;

public class TrackingStatusHistoryDto
{
    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public string ChangedBy { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }
}