using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Cekok.Api.Services;

public class DeployService(
    CekokDbContext db,
    BuildService buildSvc,
    SshService sshSvc,
    ScpService scpSvc,
    HealthCheckService healthSvc,
    EncryptionService encSvc,
    ILogger<DeployService> logger)
{
    public async Task<DeployJob> TriggerAsync(string appId, string triggeredBy,
        string? userId, IEnumerable<string> allowedServerIds, CancellationToken ct)
    {
        var app = await db.Applications.FindAsync([appId], ct)
            ?? throw new KeyNotFoundException($"App not found: {appId}");

        var targets = await db.DeployTargets
            .Where(t => t.AppId == appId && allowedServerIds.Contains(t.ServerId))
            .ToListAsync(ct);

        if (!targets.Any())
            throw new UnauthorizedAccessException("No accessible deploy targets");

        var job = new DeployJob
        {
            AppId = appId,
            TriggeredBy = triggeredBy,
            TriggeredByUser = userId,
            Status = "queued",
        };
        db.DeployJobs.Add(job);
        await db.SaveChangesAsync(ct);

        // Fire-and-forget background execution
        _ = Task.Run(() => RunJobAsync(job.Id, app, targets), CancellationToken.None);
        return job;
    }

    private async Task RunJobAsync(string jobId, Application app, List<DeployTarget> targets)
    {
        var job = await db.DeployJobs.FindAsync(jobId);
        if (job is null) return;

        async Task Log(string serverId, string level, string message)
        {
            db.DeployLogs.Add(new DeployLog
            {
                JobId = jobId,
                ServerId = serverId,
                Level = level,
                Message = message
            });
            await db.SaveChangesAsync();
        }

        try
        {
            job.Status = "running";
            job.StartedAt = DateTime.UtcNow.ToString("O");
            await db.SaveChangesAsync();

            // [3] Build
            var outputPath = await buildSvc.BuildAsync(app, jobId,
                (level, msg) => Log(null!, level, msg).Wait(),
                CancellationToken.None);

            // [5] Deploy to all targets in parallel
            var deployTasks = targets.Select(t => DeployToTargetAsync(job, app, t, outputPath, Log));
            await Task.WhenAll(deployTasks);

            var allSuccess = targets.All(t => t.Status == "success");
            job.Status = allSuccess ? "success" : "failed";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deploy job {JobId} failed", jobId);
            job.Status = "failed";
            await Log(null!, "error", $"✗ {ex.Message}");
        }
        finally
        {
            job.FinishedAt = DateTime.UtcNow.ToString("O");
            await db.SaveChangesAsync();

            // [6] Cleanup
            buildSvc.Cleanup(app.Id, jobId);
        }
    }

    private async Task DeployToTargetAsync(DeployJob job, Application app, DeployTarget target,
        string localOutputPath, Func<string, string, string, Task> log)
    {
        var server = await db.Servers.FindAsync(target.ServerId);
        if (server is null) return;

        var password = encSvc.Decrypt(server.SshPasswordEnc);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var backupDir = $"{target.DeployDir}.bak.{timestamp}";

        try
        {
            await log(server.Id, "cmd", $"$ ssh {server.SshUser}@{server.Ip} mv {target.DeployDir} {backupDir}");
            await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                $"mv {target.DeployDir} {backupDir} 2>/dev/null; mkdir -p {target.DeployDir}");

            await log(server.Id, "cmd", $"$ scp -r {localOutputPath}/ {server.SshUser}@{server.Ip}:{target.DeployDir}/");
            await scpSvc.UploadDirectoryAsync(server.Ip, server.SshPort, server.SshUser, password,
                localOutputPath, target.DeployDir);
            await log(server.Id, "success", "✓ Upload complete");

            if (!string.IsNullOrEmpty(target.ServiceName))
            {
                await log(server.Id, "cmd", $"$ ssh {server.SshUser}@{server.Ip} systemctl restart {target.ServiceName}");
                await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                    $"systemctl restart {target.ServiceName}");
                await Task.Delay(3000);
            }

            // Health check
            if (!string.IsNullOrEmpty(target.HealthCheckUrl))
            {
                var ok = await healthSvc.CheckAsync(target.HealthCheckUrl);
                if (!ok) throw new Exception($"Health check failed: {target.HealthCheckUrl}");
                await log(server.Id, "success", $"✓ Health check OK");
            }

            target.Status = "success";
            await log(server.Id, "success", $"✓ Deploy complete");
        }
        catch (Exception ex)
        {
            await log(server.Id, "error", $"✗ Deploy failed: {ex.Message}");
            await log(server.Id, "error", "↩ Auto-rollback triggered");

            // Rollback
            try
            {
                await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                    $"rm -rf {target.DeployDir}; mv {backupDir} {target.DeployDir}");
                if (!string.IsNullOrEmpty(target.ServiceName))
                    await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                        $"systemctl restart {target.ServiceName}");
                await log(server.Id, "warn", "✓ Rollback complete");
            }
            catch (Exception rex)
            {
                await log(server.Id, "error", $"✗ Rollback failed: {rex.Message}");
            }

            target.Status = "failed";
        }
    }

    public async Task<DeployJob?> GetStatusAsync(string appId, CancellationToken ct) =>
        await db.DeployJobs
            .Where(j => j.AppId == appId)
            .OrderByDescending(j => j.StartedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<List<DeployLog>> GetLogsAsync(string jobId, CancellationToken ct) =>
        await db.DeployLogs.Where(l => l.JobId == jobId).OrderBy(l => l.Id).ToListAsync(ct);

    public async Task<List<DeployJob>> GetHistoryAsync(int page, int size, CancellationToken ct) =>
        await db.DeployJobs
            .OrderByDescending(j => j.StartedAt)
            .Skip(page * size).Take(size)
            .ToListAsync(ct);
}
