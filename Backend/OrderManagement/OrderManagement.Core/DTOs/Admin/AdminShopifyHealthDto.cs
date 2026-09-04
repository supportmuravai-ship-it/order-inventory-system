namespace OrderManagement.Core.DTOs.Admin;

public class AdminShopifyHealthDto
{
    public int StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string StoreCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string? ShopDomain { get; set; }

    public string ConnectionStatus { get; set; } = string.Empty;

    public DateTime? ShopifyConnectedAtUtc { get; set; }

    public DateTime? LastSuccessfulSyncAtUtc { get; set; }

    public DateTime? LastReconciliationAtUtc { get; set; }

    public DateTime? LastWebhookReceivedAtUtc { get; set; }

    public string? LastShopifyError { get; set; }


    public void SetUtcKinds()
    {
        ShopifyConnectedAtUtc =
            ShopifyConnectedAtUtc.HasValue
                ? DateTime.SpecifyKind(
                    ShopifyConnectedAtUtc.Value,
                    DateTimeKind.Utc)
                : null;

        LastSuccessfulSyncAtUtc =
            LastSuccessfulSyncAtUtc.HasValue
                ? DateTime.SpecifyKind(
                    LastSuccessfulSyncAtUtc.Value,
                    DateTimeKind.Utc)
                : null;

        LastReconciliationAtUtc =
            LastReconciliationAtUtc.HasValue
                ? DateTime.SpecifyKind(
                    LastReconciliationAtUtc.Value,
                    DateTimeKind.Utc)
                : null;

        LastWebhookReceivedAtUtc =
            LastWebhookReceivedAtUtc.HasValue
                ? DateTime.SpecifyKind(
                    LastWebhookReceivedAtUtc.Value,
                    DateTimeKind.Utc)
                : null;
    }
}