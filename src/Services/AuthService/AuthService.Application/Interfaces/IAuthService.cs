using AuthService.Application.DTOs;
using Shared.Common.Models;

namespace AuthService.Application.Interfaces;

/// <summary>
/// Authentication servisi soyutlaması (Interface Segregation Principle).
/// </summary>
public interface IAuthService
{
    Task<ApiResponse<TokenDto>> RegisterAsync(RegisterDto registerDto);
    Task<ApiResponse<TokenDto>> LoginAsync(LoginDto loginDto);
    Task<ApiResponse<TokenDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<ApiResponse<bool>> RevokeTokenAsync(string userId);
    Task<ApiResponse<bool>> AssignRoleAsync(AssignRoleDto assignRoleDto);
}
