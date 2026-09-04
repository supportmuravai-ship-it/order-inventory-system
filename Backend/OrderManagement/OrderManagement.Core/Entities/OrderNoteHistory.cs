using OrderManagement.Core.Enums;

namespace OrderManagement.Core.Entities;

public class OrderNoteHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public NoteType NoteType { get; set; }

    public string? OldText { get; set; }

    public string? NewText { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}