using System.Text.Json.Serialization;

namespace OrderManagement.Infrastructure.Shopify.Models;

public class ShopifyGraphQlResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<ShopifyGraphQlError> Errors { get; set; } = [];
}

public class ShopifyGraphQlError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ShopifyOrdersData
{
    [JsonPropertyName("orders")]
    public ShopifyOrderConnection Orders { get; set; } = new();
}

public class ShopifyOrderConnection
{
    [JsonPropertyName("edges")]
    public List<ShopifyOrderEdge> Edges { get; set; } = [];

    [JsonPropertyName("pageInfo")]
    public ShopifyPageInfo PageInfo { get; set; } = new();
}

public class ShopifyOrderEdge
{
    [JsonPropertyName("node")]
    public ShopifyOrder Node { get; set; } = new();
}

public class ShopifyOrder
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("totalPriceSet")]
    public ShopifyMoneySet TotalPriceSet { get; set; } = new();

    [JsonPropertyName("customer")]
    public ShopifyCustomer? Customer { get; set; }

    [JsonPropertyName("shippingAddress")]
    public ShopifyAddress? ShippingAddress { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("lineItems")]
    public ShopifyLineItemConnection LineItems { get; set; } = new();

    [JsonPropertyName("customAttributes")]
    public List<ShopifyAttribute> CustomAttributes { get; set; } = [];
}

public class ShopifyCustomer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

public class ShopifyAddress
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public class ShopifyLineItemConnection
{
    [JsonPropertyName("edges")]
    public List<ShopifyLineItemEdge> Edges { get; set; } = [];
}

public class ShopifyLineItemEdge
{
    [JsonPropertyName("node")]
    public ShopifyLineItem Node { get; set; } = new();
}

public class ShopifyLineItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("sku")]
    public string? SKU { get; set; }

    [JsonPropertyName("originalUnitPriceSet")]
    public ShopifyMoneySet OriginalUnitPriceSet { get; set; } = new();

    [JsonPropertyName("variant")]
    public ShopifyVariant? Variant { get; set; }
}

public class ShopifyVariant
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("product")]
    public ShopifyProduct? Product { get; set; }
}

public class ShopifyProduct
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class ShopifyMoneySet
{
    [JsonPropertyName("shopMoney")]
    public ShopifyMoney ShopMoney { get; set; } = new();
}

public class ShopifyMoney
{
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0";

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;
}

public class ShopifyPageInfo
{
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("endCursor")]
    public string? EndCursor { get; set; }
}

public class ShopifyOrderPage
{
    public List<ShopifyOrder> Orders { get; set; } = [];
    public bool HasNextPage { get; set; }
    public string? EndCursor { get; set; }
}

public class ShopifySingleOrderData
{
    [JsonPropertyName("order")]
    public ShopifyOrder? Order { get; set; }
}

public class ShopifyWebhookCreateData
{
    [JsonPropertyName("webhookSubscriptionCreate")]
    public ShopifyWebhookCreatePayload WebhookSubscriptionCreate { get; set; } = new();
}

public class ShopifyWebhookCreatePayload
{
    [JsonPropertyName("webhookSubscription")]
    public ShopifyWebhookSubscription? WebhookSubscription { get; set; }

    [JsonPropertyName("userErrors")]
    public List<ShopifyUserError> UserErrors { get; set; } = [];
}

public class ShopifyWebhookSubscription
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class ShopifyUserError
{
    [JsonPropertyName("field")]
    public List<string>? Field { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ShopifyAttribute
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}