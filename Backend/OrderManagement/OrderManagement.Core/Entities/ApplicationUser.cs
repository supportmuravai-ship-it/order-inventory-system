using Microsoft.AspNetCore.Identity;

namespace OrderManagement.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<UserStoreAccess> StoreAccesses { get; set; }  = new List<UserStoreAccess>();
}

//ASP.NET Identity already gives us:

//Id
//UserName
//Email
//PasswordHash
//PhoneNumber
//SecurityStamp
//etc.

//So we do not create those ourselves.