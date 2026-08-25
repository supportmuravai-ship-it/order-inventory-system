namespace OrderManagement.Core.Entities;

public class UserStoreAccess
{
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int StoreId { get; set; }

    public Store Store { get; set; } = null!;
}

//This gives us:

//User
//↕
//UserStoreAccess
//↕
//Store