namespace OrderManagement.Infrastructure.Shopify.Models;

public class ShopifySyncResult
{
    public int Fetched { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }

    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = [];
}