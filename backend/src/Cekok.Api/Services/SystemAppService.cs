using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cekok.Api.Services;

public class SystemAppService(CekokDbContext db, SshService sshSvc, EncryptionService encSvc)
{
    public async Task<Dictionary<string, string>> GetAllStatusesAsync(string serverId, CancellationToken ct)
    {
        var server = await db.Servers.FindAsync([serverId], ct)
            ?? throw new KeyNotFoundException("Server not found");
        var pw = encSvc.Decrypt(server.SshPasswordEnc);

        var apps = new Dictionary<string, string>
        {
            { "nginx", "systemctl is-active nginx" },
            { "redis", "systemctl is-active redis-server" },
            { "dotnet", "dotnet --version" }
        };

        var statuses = new Dictionary<string, string>();

        foreach (var app in apps)
        {
            try
            {
                var result = await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw, app.Value, ct);
                if (app.Key == "dotnet")
                {
                    statuses[app.Key] = !string.IsNullOrWhiteSpace(result) && !result.Contains("not found") ? "active" : "inactive";
                }
                else
                {
                    statuses[app.Key] = result.Trim() == "active" ? "active" : "inactive";
                }
            }
            catch
            {
                statuses[app.Key] = "inactive";
            }
        }

        return statuses;
    }

    public async Task<(string Output, string Error, int ExitStatus)> InstallAsync(string serverId, string appId, CancellationToken ct)
    {
        var server = await db.Servers.FindAsync([serverId], ct)
            ?? throw new KeyNotFoundException("Server not found");
        var pw = encSvc.Decrypt(server.SshPasswordEnc);

        string command = appId.ToLower() switch
        {
            "nginx" => "apt-get update -qq && apt-get install -y nginx && systemctl enable --now nginx",
            "redis" => "apt-get update -qq && apt-get install -y redis-server && systemctl enable --now redis-server",
            "dotnet" => "apt-get update -qq && apt-get install -y dotnet-sdk-8.0",
            _ => throw new ArgumentException("Invalid App ID")
        };

        var result = await sshSvc.RunCommandDetailedAsync(server.Ip, server.SshPort, server.SshUser, pw, command, ct);

        if (result.ExitStatus == 0 && appId.ToLower() == "nginx")
        {
            server.NginxInstalled = true;
            await db.SaveChangesAsync(ct);
        }

        return result;
    }
}
