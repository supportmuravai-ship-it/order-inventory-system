namespace OrderManagement.Core.Entities;

public class Store
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<UserStoreAccess> UserAccesses { get; set; }   = new List<UserStoreAccess>();
}

// We are intentionally not adding Shopify credentials, domains, webhooks, etc. Those belong to later phases.