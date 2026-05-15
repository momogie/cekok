namespace Cekok.Api.Services.Abstractions;

public interface ISshService
{
    Task<(string Output, string Error, int ExitStatus)> RunCommandDetailedAsync(string host, int port, string user, string password, string command, CancellationToken ct = default);
    Task<string> RunCommandAsync(string host, int port, string user, string password, string command, CancellationToken ct = default);
    IAsyncEnumerable<string> RunCommandStreamAsync(string host, int port, string user, string password, string command, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(string host, int port, string user, string password, CancellationToken ct = default);
}
