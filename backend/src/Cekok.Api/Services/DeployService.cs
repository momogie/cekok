using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Cekok.Api.Services;

public class DeployService(
    IServiceScopeFactory scopeFactory,
    CekokDbContext db,
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

        // Fire-and-forget background execution with its own service scope
        var serverIds = targets.Select(t => t.ServerId).ToList();
        _ = Task.Run(() => RunJobInNewScopeAsync(job.Id, appId, serverIds, CancellationToken.None));
        
        return job;
    }

    private async Task RunJobInNewScopeAsync(string jobId, string appId, List<string> serverIds, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        
        var scopeDb = sp.GetRequiredService<CekokDbContext>();
        var scopeBuild = sp.GetRequiredService<BuildService>();
        var scopeSsh = sp.GetRequiredService<SshService>();
        var scopeScp = sp.GetRequiredService<ScpService>();
        var scopeHealth = sp.GetRequiredService<HealthCheckService>();
        var scopeEnc = sp.GetRequiredService<EncryptionService>();
        
        var job = await scopeDb.DeployJobs.FindAsync([jobId], ct);
        var app = await scopeDb.Applications.FindAsync([appId], ct);
        var targets = await scopeDb.DeployTargets.Where(t => t.AppId == appId && serverIds.Contains(t.ServerId)).ToListAsync(ct);

        if (job is null || app is null) return;

        async Task Log(string? serverId, string level, string message)
        {
            scopeDb.DeployLogs.Add(new DeployLog
            {
                JobId = jobId,
                ServerId = serverId,
                Level = level,
                Message = message
            });
            await scopeDb.SaveChangesAsync(ct);
        }

        try
        {
            job.Status = "running";
            job.StartedAt = DateTime.UtcNow.ToString("O");
            await scopeDb.SaveChangesAsync(ct);

            // [3] Build
            var outputPath = await scopeBuild.BuildAsync(app, jobId,
                (level, msg) => Log(null!, level, msg).Wait(),
                ct);

            // [4] Inject settings files
            var configFiles = await scopeDb.AppSettingFiles.Where(f => f.AppId == app.Id).ToListAsync(ct);
            foreach (var f in configFiles)
            {
                var fullPath = Path.Combine(outputPath, f.FilePath);
                var dir = Path.GetDirectoryName(fullPath);
                if (dir != null) Directory.CreateDirectory(dir);
                
                var content = scopeEnc.Decrypt(f.ContentEnc);
                await File.WriteAllTextAsync(fullPath, content);
                await Log(null!, "info", $"✓ Injected config: {f.FilePath}");
            }

            // [5] Deploy to all targets (Run sequentially to avoid EF Core concurrency issues)
            foreach (var target in targets)
            {
                await DeployToTargetInternalAsync(scopeDb, scopeSsh, scopeScp, scopeHealth, scopeEnc, job, app, target, outputPath, Log, ct);
            }

            var allSuccess = targets.All(t => t.Status == "success");
            job.Status = allSuccess ? "success" : "failed";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deploy job {JobId} failed", jobId);
            if (job != null) job.Status = "failed";
            await Log(null!, "error", $"✗ {ex.Message}");
        }
        finally
        {
            if (job != null)
            {
                job.FinishedAt = DateTime.UtcNow.ToString("O");
                await scopeDb.SaveChangesAsync(ct);
            }

            // [6] Cleanup
            scopeBuild.Cleanup(appId, jobId);
        }
    }

    private async Task DeployToTargetInternalAsync(
        CekokDbContext db, SshService sshSvc, ScpService scpSvc, HealthCheckService healthSvc, EncryptionService encSvc,
        DeployJob job, Application app, DeployTarget target, string localOutputPath, 
        Func<string?, string, string, Task> log, CancellationToken ct)
    {
        var server = await db.Servers.FindAsync([target.ServerId], ct);
        if (server is null) return;

        var password = encSvc.Decrypt(server.SshPasswordEnc);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var backupDir = $"{target.DeployDir}.bak.{timestamp}";

        try
        {
            await log(server.Id, "cmd", $"$ ssh {server.SshUser}@{server.Ip} mv {target.DeployDir} {backupDir}");
            await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                $"mv {target.DeployDir} {backupDir} 2>/dev/null; mkdir -p {target.DeployDir}", ct);

            await log(server.Id, "cmd", $"$ scp -r {localOutputPath}/ {server.SshUser}@{server.Ip}:{target.DeployDir}/");
            await scpSvc.UploadDirectoryAsync(server.Ip, server.SshPort, server.SshUser, password,
                localOutputPath, target.DeployDir, null, ct);
            await log(server.Id, "success", "✓ Upload complete");

            if (!string.IsNullOrEmpty(target.ServiceName))
            {
                var sudoPrefix = server.SshUser == "root" ? "" : $"echo '{password.Replace("'", "'\\''")}' | sudo -S ";
                await log(server.Id, "cmd", $"$ ssh {server.SshUser}@{server.Ip} sudo systemctl restart {target.ServiceName}");
                await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                    $"{sudoPrefix}systemctl restart {target.ServiceName}", ct);
                await Task.Delay(3000, ct);
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
                    $"rm -rf {target.DeployDir}; mv {backupDir} {target.DeployDir}", ct);
                if (!string.IsNullOrEmpty(target.ServiceName))
                {
                    var sudoPrefix = server.SshUser == "root" ? "" : $"echo '{password.Replace("'", "'\\''")}' | sudo -S ";
                    await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                        $"{sudoPrefix}systemctl restart {target.ServiceName}", ct);
                }
                await log(server.Id, "warn", "✓ Rollback complete");
            }
            catch (Exception rex)
            {
                await log(server.Id, "error", $"✗ Rollback failed: {rex.Message}");
            }

            target.Status = "failed";
        }
        
        await db.SaveChangesAsync(ct);
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

    public async Task<List<DeployJob>> GetAppHistoryAsync(string appId, int page, int size, CancellationToken ct) =>
        await db.DeployJobs
            .Where(j => j.AppId == appId)
            .OrderByDescending(j => j.StartedAt)
            .Skip(page * size).Take(size)
            .ToListAsync(ct);
}
