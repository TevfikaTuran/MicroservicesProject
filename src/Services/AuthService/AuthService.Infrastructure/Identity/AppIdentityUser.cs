using Microsoft.AspNetCore.Identity;

namespace AuthService.Infrastructure.Identity;

/// <summary>
/// Microsoft Identity ile genişletilmiş kullanıcı modeli.
/// </summary>
public class AppIdentityUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
