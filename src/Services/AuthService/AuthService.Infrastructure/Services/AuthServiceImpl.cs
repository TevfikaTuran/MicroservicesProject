using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Common.Models;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// IAuthService implementasyonu. Open/Closed Principle: Yeni auth yöntemleri eklenebilir.
/// </summary>
public class AuthServiceImpl : IAuthService
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthServiceImpl> _logger;

    public AuthServiceImpl(
        UserManager<AppIdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        AuthDbContext context,
        IConfiguration configuration,
        ILogger<AuthServiceImpl> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<TokenDto>> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Kayıt başarısız - Email zaten kayıtlı: {Email}", registerDto.Email);
            return ApiResponse<TokenDto>.FailResult("Bu email adresi zaten kayıtlı");
        }

        var user = new AppIdentityUser
        {
            UserName = registerDto.UserName,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<TokenDto>.FailResult("Kullanıcı oluşturulamadı", errors);
        }

        if (!await _roleManager.RoleExistsAsync(Roles.User))
            await _roleManager.CreateAsync(new IdentityRole(Roles.User));
        await _userManager.AddToRoleAsync(user, Roles.User);

        var tokenDto = await GenerateTokensAsync(user);
        _logger.LogInformation("Yeni kullanıcı kayıt oldu: {Email}", registerDto.Email);
        return ApiResponse<TokenDto>.SuccessResult(tokenDto, "Kayıt başarılı");
    }

    public async Task<ApiResponse<TokenDto>> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            _logger.LogWarning("Başarısız giriş: {Email}", loginDto.Email);
            return ApiResponse<TokenDto>.FailResult("Geçersiz email veya şifre");
        }

        if (!user.IsActive)
            return ApiResponse<TokenDto>.FailResult("Hesabınız devre dışı");

        var tokenDto = await GenerateTokensAsync(user);
        _logger.LogInformation("Kullanıcı giriş yaptı: {Email}", loginDto.Email);
        return ApiResponse<TokenDto>.SuccessResult(tokenDto, "Giriş başarılı");
    }

    public async Task<ApiResponse<TokenDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
        if (principal == null)
            return ApiResponse<TokenDto>.FailResult("Geçersiz access token");

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return ApiResponse<TokenDto>.FailResult("Token'dan kullanıcı bilgisi alınamadı");

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken && rt.UserId == userId);

        if (storedToken == null || !storedToken.IsActive)
            return ApiResponse<TokenDto>.FailResult("Geçersiz veya süresi dolmuş refresh token");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse<TokenDto>.FailResult("Kullanıcı bulunamadı");

        // Token Rotation
        storedToken.IsUsed = true;
        var tokenDto = await GenerateTokensAsync(user);
        storedToken.ReplacedByToken = tokenDto.RefreshToken;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Token yenilendi. UserId: {UserId}", userId);
        return ApiResponse<TokenDto>.SuccessResult(tokenDto, "Token yenilendi");
    }

    public async Task<ApiResponse<bool>> RevokeTokenAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync();
        foreach (var token in tokens) token.IsRevoked = true;
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResult(true, "Token'lar iptal edildi");
    }

    public async Task<ApiResponse<bool>> AssignRoleAsync(AssignRoleDto assignRoleDto)
    {
        var user = await _userManager.FindByEmailAsync(assignRoleDto.Email);
        if (user == null) return ApiResponse<bool>.FailResult("Kullanıcı bulunamadı");

        if (!await _roleManager.RoleExistsAsync(assignRoleDto.RoleName))
            await _roleManager.CreateAsync(new IdentityRole(assignRoleDto.RoleName));

        var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);
        if (!result.Succeeded)
            return ApiResponse<bool>.FailResult("Rol ataması başarısız", result.Errors.Select(e => e.Description).ToList());

        _logger.LogInformation("Rol atandı: {Role} -> {Email}", assignRoleDto.RoleName, assignRoleDto.Email);
        return ApiResponse<bool>.SuccessResult(true, "Rol başarıyla atandı");
    }

    private async Task<TokenDto> GenerateTokensAsync(AppIdentityUser user)
    {
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpDays)
        };

        await _context.RefreshTokens.AddAsync(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60")),
            RefreshTokenExpiration = refreshTokenEntity.ExpiresAt
        };
    }
}
