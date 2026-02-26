namespace AuthService.Domain.Entities;

/// <summary>
/// Kullanıcı domain entity'si. Domain katmanı hiçbir dış bağımlılığa sahip değildir (Onion Architecture).
/// </summary>
public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
