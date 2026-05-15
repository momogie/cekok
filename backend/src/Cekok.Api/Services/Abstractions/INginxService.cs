namespace Cekok.Api.Services.Abstractions;

public interface INginxService
{
    Task<string> GetStatusAsync(string serverId, CancellationToken ct);
    Task InstallAsync(string serverId, CancellationToken ct);
    Task ReloadAsync(string serverId, CancellationToken ct);
    Task DeployConfigAsync(string serverId, string siteName, string templateType, string content, CancellationToken ct);
}
