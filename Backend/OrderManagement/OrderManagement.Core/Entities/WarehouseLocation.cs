namespace OrderManagement.Core.Entities;

public class WarehouseLocation
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

// This is intentionally only a lightweight warehouse foundation. No stock/inventory logic belongs in Phase 2.