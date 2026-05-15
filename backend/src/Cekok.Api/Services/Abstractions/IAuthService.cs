using Cekok.Api.Dtos;

namespace Cekok.Api.Services.Abstractions;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<LoginResponse?> RefreshAsync(RefreshRequest req, CancellationToken ct);
    Task<bool> LogoutAsync(string refreshToken, CancellationToken ct);
}
