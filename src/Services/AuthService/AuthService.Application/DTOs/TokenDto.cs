namespace AuthService.Application.DTOs;

public record TokenDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiration { get; init; }
    public DateTime RefreshTokenExpiration { get; init; }
}
