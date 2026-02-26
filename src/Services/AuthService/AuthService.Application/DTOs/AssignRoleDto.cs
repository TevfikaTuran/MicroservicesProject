namespace AuthService.Application.DTOs;

public record AssignRoleDto
{
    public string Email { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
}
