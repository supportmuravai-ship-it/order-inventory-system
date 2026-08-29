namespace OrderManagement.Core.Entities;

public class Customer
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public string? ExternalCustomerId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = "UAE"; // Country Hardcoded

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Store Store { get; set; } = null!;

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}