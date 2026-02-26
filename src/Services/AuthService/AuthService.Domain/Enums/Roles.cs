namespace AuthService.Domain.Enums;

/// <summary>
/// Role-Based Authorization için tanımlanan roller ve politikalar.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Manager = "Manager";
}

public static class Policies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireManager = "RequireManager";
    public const string CanManageProducts = "CanManageProducts";
}
