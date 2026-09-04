using OrderManagement.Core.Enums;

namespace OrderManagement.Core.DTOs.Orders;

public class OrderNoteHistoryDto
{
    public NoteType NoteType { get; set; }

    public string? OldText { get; set; }

    public string? NewText { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public string ChangedBy { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }
}