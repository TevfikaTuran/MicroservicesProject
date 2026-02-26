using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

/// <summary>
/// Kimlik doğrulama ve yetkilendirme controller'ı.
/// JWT token üretimi, yenileme, iptal ve rol atama endpoint'leri.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// POST api/auth/register - Yeni kullanıcı kaydı
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// POST api/auth/login - Kullanıcı girişi, JWT token döndürür
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// POST api/auth/refresh-token - Access token'ı refresh token ile yeniler
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var result = await _authService.RefreshTokenAsync(refreshTokenDto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// POST api/auth/revoke - Refresh token'ı iptal eder (JWT gerektirir)
    /// </summary>
    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await _authService.RevokeTokenAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// POST api/auth/assign-role - Kullanıcıya rol atar (Admin yetkisi gerekir)
    /// Policy-Based Authorization: RequireAdmin politikası.
    /// </summary>
    [HttpPost("assign-role")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto assignRoleDto)
    {
        var result = await _authService.AssignRoleAsync(assignRoleDto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
