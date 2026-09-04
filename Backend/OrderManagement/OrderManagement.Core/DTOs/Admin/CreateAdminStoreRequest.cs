namespace OrderManagement.Core.DTOs.Admin;

public class CreateAdminStoreRequest
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string ShopDomain { get; set; } = string.Empty;
}