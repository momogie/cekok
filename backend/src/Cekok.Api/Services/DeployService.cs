using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text.Json;

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

        var escapedDeployDir = $"\"{target.DeployDir}\"";
        var escapedBackupDir = $"\"{backupDir}\"";
        var sudoCmd = server.SshUser == "root" ? "" : $"echo '{password.Replace("'", "'\\''")}' | sudo -S ";

        try
        {
            // Ensure directory exists, then move to backup (if exists), then recreate empty target dir as owned by SSH user
            await log(server.Id, "cmd", $"$ mkdir -p {escapedDeployDir} && mv {escapedDeployDir} {escapedBackupDir}");
            await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                $"{sudoCmd}bash -c \"if [ -d {escapedDeployDir} ]; then mv {escapedDeployDir} {escapedBackupDir} 2>/dev/null; fi; mkdir -p {escapedDeployDir} && chown {server.SshUser}:{server.SshUser} {escapedDeployDir}\"", ct);

            await log(server.Id, "cmd", $"$ scp -r {localOutputPath}/ {server.SshUser}@{server.Ip}:{escapedDeployDir}/");
            await scpSvc.UploadDirectoryAsync(server.Ip, server.SshPort, server.SshUser, password,
                localOutputPath, target.DeployDir, null, ct);
            await log(server.Id, "success", "✓ Upload complete");

            if (!string.IsNullOrEmpty(target.ServiceName))
            {
                await log(server.Id, "cmd", $"$ sudo systemctl restart {target.ServiceName}");
                try
                {
                    await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                        $"{sudoCmd}systemctl restart {target.ServiceName}", ct);
                }
                catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    await log(server.Id, "warn", $"⚠ Service {target.ServiceName} not found. Attempting to create it...");
                    await CreateServiceFileAsync(sshSvc, server, password, app, target, sudoCmd, log, ct);
                    await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
                        $"{sudoCmd}systemctl restart {target.ServiceName}", ct);
                }
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
                    $"{sudoCmd}bash -c \"rm -rf {escapedDeployDir}; if [ -d {escapedBackupDir} ]; then mv {escapedBackupDir} {escapedDeployDir}; fi\"", ct);
                if (!string.IsNullOrEmpty(target.ServiceName))
                {
                    // Ignore service restart failures during rollback
                    await sshSvc.RunCommandDetailedAsync(server.Ip, server.SshPort, server.SshUser, password,
                        $"{sudoCmd}systemctl restart {target.ServiceName}", ct);
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

    private async Task CreateServiceFileAsync(SshService sshSvc, Server server, string password, 
        Application app, DeployTarget target, string sudoCmd,
        Func<string?, string, string, Task> log, CancellationToken ct)
    {
        var serviceFile = $"/etc/systemd/system/{target.ServiceName}.service";
        var deployDir = target.DeployDir.Replace("\\", "/");
        
        var startCmd = app.Type.ToLower() switch {
            "dotnet" => $"/usr/bin/env dotnet \"{deployDir}/{app.Name}.dll\"",
            "next" or "nuxt" => $"/usr/bin/env node \"{deployDir}/server/index.mjs\"",
            _ => $"# Please configure start command for {app.Type}"
        };

        var envVarsString = "";
        if (target.Port.HasValue) {
            envVarsString += $"Environment=ASPNETCORE_URLS=http://*:{target.Port}\n";
            envVarsString += $"Environment=PORT={target.Port}\n";
        }
        
        if (!string.IsNullOrEmpty(app.EnvVars)) {
             try {
                 using var doc = JsonDocument.Parse(app.EnvVars);
                 foreach (var item in doc.RootElement.EnumerateArray()) {
                     var k = item.GetProperty("key").GetString();
                     var v = item.GetProperty("val").GetString();
                     envVarsString += $"Environment={k}={v}\n";
                 }
             } catch {}
        }

        var content = $"""
[Unit]
Description=Cekok Service: {app.Name}
After=network.target

[Service]
Type=simple
User={server.SshUser}
WorkingDirectory="{deployDir}"
ExecStart={startCmd}
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier={target.ServiceName}
{envVarsString}

[Install]
WantedBy=multi-user.target
""";

        await log(server.Id, "info", $"Service file content generated for {target.ServiceName}");
        var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));
        await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, password,
            $"{sudoCmd}bash -c \"echo '{base64Content}' | base64 -d > {serviceFile} && systemctl daemon-reload && systemctl enable {target.ServiceName}\"", ct);
        await log(server.Id, "success", $"✓ Created systemd service: {target.ServiceName}");
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
