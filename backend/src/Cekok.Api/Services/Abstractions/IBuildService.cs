using Cekok.Api.Models.Entities;

namespace Cekok.Api.Services.Abstractions;

public interface IBuildService
{
    Task<string> BuildAsync(Application app, string jobId, Action<string, string> logCallback, CancellationToken ct);
    void Cleanup(string appId, string jobId);
}
