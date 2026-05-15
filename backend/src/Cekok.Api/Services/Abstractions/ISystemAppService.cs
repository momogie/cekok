namespace Cekok.Api.Services.Abstractions;

public interface ISystemAppService
{
    Task<Dictionary<string, string>> GetAllStatusesAsync(string serverId, CancellationToken ct);
    IAsyncEnumerable<string> InstallStreamAsync(string serverId, string appId, CancellationToken ct);
}
