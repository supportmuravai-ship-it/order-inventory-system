namespace OrderManagement.Core.Entities;

public class Store
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    // Example: my-store.myshopify.com
    // Non-secret Shopify store identity.
    public string? ShopDomain { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<UserStoreAccess> UserAccesses { get; set; }
        = new List<UserStoreAccess>();

    public ICollection<Customer> Customers { get; set; }
        = new List<Customer>();

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();

    public DateTime? LastSuccessfulSyncAtUtc { get; set; }

    public DateTime? LastReconciliationAtUtc { get; set; }

    public DateTime? LastWebhookReceivedAtUtc { get; set; }

    public string? LastShopifyError { get; set; }
}