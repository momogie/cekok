using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Cekok.Api.Services;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, UserDto User);
public record RefreshRequest(string RefreshToken);
public record UserDto(string Id, string Username, string DisplayName, string Role);

public class AuthService(CekokDbContext db, IConfiguration config)
{
    private readonly string _jwtSecret =
        Environment.GetEnvironmentVariable("CEKOK_JWT_SECRET")
        ?? config["Jwt:Secret"]
        ?? throw new Exception("JWT Secret not configured");

    public async Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.Username == req.Username && u.IsActive, ct);
        if (user is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)) return null;

        user.LastLoginAt = DateTime.UtcNow.ToString("O");
        await db.SaveChangesAsync(ct);

        var access = GenerateAccessToken(user);
        var refresh = await GenerateRefreshTokenAsync(user.Id, ct);
        return new LoginResponse(access, refresh, ToDto(user));
    }

    public async Task<LoginResponse?> RefreshAsync(RefreshRequest req, CancellationToken ct)
    {
        var hash = HashToken(req.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t =>
            t.TokenHash == hash && t.RevokedAt == null, ct);
        if (token is null) return null;
        if (DateTime.Parse(token.ExpiresAt) < DateTime.UtcNow) return null;

        var user = await db.Users.FindAsync([token.UserId], ct);
        if (user is null || !user.IsActive) return null;

        // Rotate token
        token.RevokedAt = DateTime.UtcNow.ToString("O");
        var newRefresh = await GenerateRefreshTokenAsync(user.Id, ct);
        var access = GenerateAccessToken(user);
        await db.SaveChangesAsync(ct);

        return new LoginResponse(access, newRefresh, ToDto(user));
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = HashToken(refreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null) return false;
        token.RevokedAt = DateTime.UtcNow.ToString("O");
        await db.SaveChangesAsync(ct);
        return true;
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(
            config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 15));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("displayName", user.DisplayName),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(string userId, CancellationToken ct)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashToken(raw);
        var days = config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(days).ToString("O"),
        });
        await db.SaveChangesAsync(ct);
        return raw;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static UserDto ToDto(User u) =>
        new(u.Id, u.Username, u.DisplayName, u.Role.ToString());
}
