namespace OrderManagement.Infrastructure.Shopify;

public class ShopifyOAuthOptions
{
    public const string SectionName = "Shopify";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string AppBaseUrl { get; set; } = string.Empty;

    public string Scopes { get; set; } = "read_orders,read_customers,read_products";
}