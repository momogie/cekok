using Cekok.Api.Services.Abstractions;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Cekok.Api.Jobs;

public class ScheduledDeployJob(IDeployService deploySvc, ILogger<ScheduledDeployJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(string appId, string[] allowedServerIds)
    {
        logger.LogInformation("Scheduled deploy triggered for app {AppId}", appId);
        try
        {
            var job = await deploySvc.TriggerAsync(appId, "schedule", null, allowedServerIds,
                CancellationToken.None);
            logger.LogInformation("Scheduled deploy job created: {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled deploy failed for app {AppId}", appId);
            throw;
        }
    }
}
