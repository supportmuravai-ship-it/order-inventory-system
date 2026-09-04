namespace OrderManagement.Core.DTOs.Admin;

public class AdminUserListItemDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<int> StoreIds { get; set; } = [];
    public List<string> Roles { get; set; } = [];

    public List<string> Stores { get; set; } = [];
}