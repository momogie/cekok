namespace Cekok.Api.Services.Abstractions;

public interface IHealthCheckService
{
    Task<bool> CheckAsync(string url, CancellationToken ct = default);
}
