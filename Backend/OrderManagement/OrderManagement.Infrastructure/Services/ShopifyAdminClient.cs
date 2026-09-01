using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OrderManagement.Infrastructure.Shopify.Models;

namespace OrderManagement.Infrastructure.Services;

public class ShopifyAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ShopifyAdminClient(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<ShopifyOrder>> GetRecentOrdersAsync(
        int storeId,
        string shopDomain,
        CancellationToken cancellationToken = default)
    {
        var clientId = _configuration[
            $"Shopify:Stores:{storeId}:ClientId"];

        var clientSecret = _configuration[
            $"Shopify:Stores:{storeId}:ClientSecret"];

        var apiVersion = _configuration[
            "Shopify:ApiVersion"];

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                $"Shopify credentials are not configured for Store {storeId}.");
        }

        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            throw new InvalidOperationException(
                "Shopify API version is not configured.");
        }

        var accessToken = await GetAccessTokenAsync(
            shopDomain,
            clientId,
            clientSecret,
            cancellationToken);

        // GraphQL query
        const string query = """
        query {
          orders(first: 5, reverse: true) {
            edges {
              node {
                id
                name
                createdAt
                updatedAt

                totalPriceSet {
                  shopMoney {
                    amount
                    currencyCode
                  }
                }
                customer {
                  id
                  firstName
                  lastName
                  phone
                }
                shippingAddress {
                  firstName
                  lastName
                  phone
                  address1
                  city
                  country
                }
                lineItems(first: 20) {
                  edges {
                    node {
                      id
                      name
                      quantity
                      sku
                      originalUnitPriceSet {
                        shopMoney {
                          amount
                          currencyCode
                        }
                      }
                      variant {
                        id
                        title
                        product {
                          id
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://{shopDomain}/admin/api/{apiVersion}/graphql.json");

        request.Headers.Add(
            "X-Shopify-Access-Token",
            accessToken);

        request.Content = new StringContent(
            JsonSerializer.Serialize(new { query }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(
    cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Shopify API request failed with status {(int)response.StatusCode}.");
        }

        var graphQlResponse =
            JsonSerializer.Deserialize<
                ShopifyGraphQlResponse<ShopifyOrdersData>>(
                responseText);

        if (graphQlResponse is null)
        {
            throw new InvalidOperationException(
                "Shopify returned an invalid response.");
        }

        if (graphQlResponse.Errors.Count > 0)
        {
            var errorMessages = string.Join(
                " | ",
                graphQlResponse.Errors.Select(x => x.Message));

            throw new InvalidOperationException(
                $"Shopify GraphQL error: {errorMessages}");
        }

        if (graphQlResponse.Data is null)
        {
            throw new InvalidOperationException(
                "Shopify response did not contain order data.");
        }

        return graphQlResponse.Data.Orders.Edges
            .Select(x => x.Node)
            .ToList();
    }

    private async Task<string> GetAccessTokenAsync(
        string shopDomain,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://{shopDomain}/admin/oauth/access_token");

        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to obtain Shopify access token. Status {(int)response.StatusCode}.");
        }

        using var json = JsonDocument.Parse(responseText);

        if (!json.RootElement.TryGetProperty(
                "access_token",
                out var accessTokenElement))
        {
            throw new InvalidOperationException(
                "Shopify access-token response did not contain an access token.");
        }

        return accessTokenElement.GetString()
            ?? throw new InvalidOperationException(
                "Shopify returned an empty access token.");
    }

    public async Task<ShopifyOrderPage> GetOrdersPageAsync(
    int storeId,
    string shopDomain,
    int first,
    string? after = null,
    CancellationToken cancellationToken = default)
    {
        var clientId = _configuration[$"Shopify:Stores:{storeId}:ClientId"];
        var clientSecret = _configuration[$"Shopify:Stores:{storeId}:ClientSecret"];
        var apiVersion = _configuration["Shopify:ApiVersion"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException($"Shopify credentials are not configured for Store {storeId}.");
        }

        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            throw new InvalidOperationException("Shopify API version is not configured.");
        }

        var accessToken = await GetAccessTokenAsync(shopDomain, clientId, clientSecret, cancellationToken);

        const string query = """
    query GetOrders($first: Int!, $after: String) {
      orders(first: $first, after: $after, reverse: true) {
        edges {
          node {
            id
            name
            createdAt
            updatedAt

                customAttributes {
      key
      value
    }

            totalPriceSet {
              shopMoney {
                amount
                currencyCode
              }
            }
            customer {
              id
              firstName
              lastName
              phone
            }
            shippingAddress {
              firstName
              lastName
              phone
              address1
              city
              country
            }
            lineItems(first: 100) {
              edges {
                node {
                  id
                  name
                  quantity
                  sku
                  originalUnitPriceSet {
                    shopMoney {
                      amount
                      currencyCode
                    }
                  }
                  variant {
                    id
                    title
                    product {
                      id
                    }
                  }
                }
              }
            }
          }
        }
        pageInfo {
          hasNextPage
          endCursor
        }
      }
    }
    """;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{shopDomain}/admin/api/{apiVersion}/graphql.json");

        request.Headers.Add("X-Shopify-Access-Token", accessToken);

        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                query,
                variables = new
                {
                    first,
                    after
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Shopify API request failed with status {(int)response.StatusCode}.");
        }

        var graphQlResponse = JsonSerializer.Deserialize<ShopifyGraphQlResponse<ShopifyOrdersData>>(responseText);

        if (graphQlResponse is null)
        {
            throw new InvalidOperationException("Shopify returned an invalid response.");
        }

        if (graphQlResponse.Errors.Count > 0)
        {
            var errorMessages = string.Join(" | ", graphQlResponse.Errors.Select(x => x.Message));
            throw new InvalidOperationException($"Shopify GraphQL error: {errorMessages}");
        }

        if (graphQlResponse.Data is null)
        {
            throw new InvalidOperationException("Shopify response did not contain order data.");
        }

        var connection = graphQlResponse.Data.Orders;

        return new ShopifyOrderPage
        {
            Orders = connection.Edges.Select(x => x.Node).ToList(),
            HasNextPage = connection.PageInfo.HasNextPage,
            EndCursor = connection.PageInfo.EndCursor
        };
    }

    public async Task<ShopifyOrder?> GetOrderByIdAsync(int storeId, string shopDomain, string externalOrderId, CancellationToken cancellationToken = default)
    {
        var clientId = _configuration[$"Shopify:Stores:{storeId}:ClientId"];
        var clientSecret = _configuration[$"Shopify:Stores:{storeId}:ClientSecret"];
        var apiVersion = _configuration["Shopify:ApiVersion"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException($"Shopify credentials are not configured for Store {storeId}.");
        }

        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            throw new InvalidOperationException("Shopify API version is not configured.");
        }

        var accessToken = await GetAccessTokenAsync(shopDomain, clientId, clientSecret, cancellationToken);

        const string query = """
    query GetOrder($id: ID!) {
      order(id: $id) {
        id
        name
        createdAt
        updatedAt

            customAttributes {
      key
      value
    }

        totalPriceSet {
          shopMoney {
            amount
            currencyCode
          }
        }
        customer {
          id
          firstName
          lastName
          phone
        }

        shippingAddress {
          firstName
          lastName
          phone
          address1
          city
          country
        }
        lineItems(first: 100) {
          edges {
            node {
              id
              name
              quantity
              sku
              originalUnitPriceSet {
                shopMoney {
                  amount
                  currencyCode
                }
              }
              variant {
                id
                title
                product {
                  id
                }
              }
            }
          }
        }
      }
    }
    """;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{shopDomain}/admin/api/{apiVersion}/graphql.json");

        request.Headers.Add("X-Shopify-Access-Token", accessToken);

        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                query,
                variables = new
                {
                    id = externalOrderId
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Shopify API request failed with status {(int)response.StatusCode}.");
        }

        var graphQlResponse = JsonSerializer.Deserialize<ShopifyGraphQlResponse<ShopifySingleOrderData>>(responseText);

        if (graphQlResponse is null)
        {
            throw new InvalidOperationException("Shopify returned an invalid response.");
        }

        if (graphQlResponse.Errors.Count > 0)
        {
            var errorMessages = string.Join(" | ", graphQlResponse.Errors.Select(x => x.Message));
            throw new InvalidOperationException($"Shopify GraphQL error: {errorMessages}");
        }

        return graphQlResponse.Data?.Order;
    }

    public async Task<ShopifyOrderPage> GetUpdatedOrdersPageAsync(
    int storeId,
    string shopDomain,
    DateTime updatedSinceUtc,
    int first,
    string? after = null,
    CancellationToken cancellationToken = default)
    {
        var clientId = _configuration[$"Shopify:Stores:{storeId}:ClientId"];
        var clientSecret = _configuration[$"Shopify:Stores:{storeId}:ClientSecret"];
        var apiVersion = _configuration["Shopify:ApiVersion"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException($"Shopify credentials are not configured for Store {storeId}.");
        }

        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            throw new InvalidOperationException("Shopify API version is not configured.");
        }

        var accessToken = await GetAccessTokenAsync(shopDomain, clientId, clientSecret, cancellationToken);

        const string query = """
    query GetUpdatedOrders($first: Int!, $after: String, $searchQuery: String!) {
      orders(first: $first, after: $after, query: $searchQuery, sortKey: UPDATED_AT) {
        edges {
          node {
            id
            name
            createdAt
            updatedAt

            customAttributes {
              key
              value
            }

            totalPriceSet {
              shopMoney {
                amount
                currencyCode
              }
            }

            customer {
              id
              firstName
              lastName
              phone
            }

            shippingAddress {
              firstName
              lastName
              phone
              address1
              city
              country
            }

            lineItems(first: 100) {
              edges {
                node {
                  id
                  name
                  quantity
                  sku

                  originalUnitPriceSet {
                    shopMoney {
                      amount
                      currencyCode
                    }
                  }

                  variant {
                    id
                    title

                    product {
                      id
                    }
                  }
                }
              }
            }
          }
        }

        pageInfo {
          hasNextPage
          endCursor
        }
      }
    }
    """;

        var searchQuery = $"updated_at:>={updatedSinceUtc:yyyy-MM-ddTHH:mm:ssZ}";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{shopDomain}/admin/api/{apiVersion}/graphql.json");

        request.Headers.Add("X-Shopify-Access-Token", accessToken);

        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                query,
                variables = new
                {
                    first,
                    after,
                    searchQuery
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Shopify API request failed with status {(int)response.StatusCode}.");
        }

        var graphQlResponse = JsonSerializer.Deserialize<ShopifyGraphQlResponse<ShopifyOrdersData>>(responseText);

        if (graphQlResponse is null)
        {
            throw new InvalidOperationException("Shopify returned an invalid response.");
        }

        if (graphQlResponse.Errors.Count > 0)
        {
            var errors = string.Join(" | ", graphQlResponse.Errors.Select(x => x.Message));
            throw new InvalidOperationException($"Shopify GraphQL error: {errors}");
        }

        if (graphQlResponse.Data is null)
        {
            throw new InvalidOperationException("Shopify response did not contain order data.");
        }

        var connection = graphQlResponse.Data.Orders;

        return new ShopifyOrderPage
        {
            Orders = connection.Edges.Select(x => x.Node).ToList(),
            HasNextPage = connection.PageInfo.HasNextPage,
            EndCursor = connection.PageInfo.EndCursor
        };
    }
}