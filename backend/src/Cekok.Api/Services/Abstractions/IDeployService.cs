using Cekok.Api.Models.Entities;

namespace Cekok.Api.Services.Abstractions;

public interface IDeployService
{
    Task<DeployJob> TriggerAsync(string appId, string triggeredBy, string? userId, IEnumerable<string> allowedServerIds, CancellationToken ct);
    Task<DeployJob?> GetStatusAsync(string appId, CancellationToken ct);
    Task<List<DeployLog>> GetLogsAsync(string jobId, CancellationToken ct);
    Task<List<DeployJob>> GetHistoryAsync(int page, int size, CancellationToken ct);
    Task<List<DeployJob>> GetAppHistoryAsync(string appId, int page, int size, CancellationToken ct);
}
