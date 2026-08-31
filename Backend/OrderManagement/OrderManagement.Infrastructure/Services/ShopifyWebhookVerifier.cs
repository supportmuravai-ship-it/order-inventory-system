using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace OrderManagement.Infrastructure.Services;

public class ShopifyWebhookVerifier
{
    private readonly IConfiguration _configuration;

    public ShopifyWebhookVerifier(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsValid(int storeId, string rawBody, string receivedHmac)
    {
        var clientSecret = _configuration[$"Shopify:Stores:{storeId}:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(receivedHmac))
        {
            return false;
        }

        var keyBytes = Encoding.UTF8.GetBytes(clientSecret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

        using var hmac = new HMACSHA256(keyBytes);
        var calculatedHash = hmac.ComputeHash(bodyBytes);

        byte[] receivedHash;

        try
        {
            receivedHash = Convert.FromBase64String(receivedHmac);
        }
        catch (FormatException)
        {
            return false;
        }

        return calculatedHash.Length == receivedHash.Length &&
               CryptographicOperations.FixedTimeEquals(calculatedHash, receivedHash);
    }
}