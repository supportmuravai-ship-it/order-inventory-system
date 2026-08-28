using OrderManagement.Core.Enums;

namespace OrderManagement.Core.Entities;

public class OrderNote
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public NoteType NoteType { get; set; }

    public string Text { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}