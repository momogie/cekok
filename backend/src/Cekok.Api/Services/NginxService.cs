using Cekok.Api.Data;
using Cekok.Api.Models;
using Cekok.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Cekok.Api.Services;

public class NginxService(CekokDbContext db, SshService sshSvc, EncryptionService encSvc)
{
    public async Task<string> GetStatusAsync(string serverId, CancellationToken ct)
    {
        var server = await db.Servers.FindAsync([serverId], ct)
            ?? throw new KeyNotFoundException("Server not found");
        var pw = encSvc.Decrypt(server.SshPasswordEnc);
        var result = await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw,
            "systemctl is-active nginx", ct);
        return result.Trim();
    }

    public async Task InstallAsync(string serverId, CancellationToken ct)
    {
        var server = await db.Servers.FindAsync([serverId], ct)
            ?? throw new KeyNotFoundException("Server not found");
        var pw = encSvc.Decrypt(server.SshPasswordEnc);
        await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw,
            "apt-get update -qq && apt-get install -y nginx && systemctl enable nginx", ct);
        server.NginxInstalled = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReloadAsync(string serverId, CancellationToken ct)
    {
        var server = await db.Servers.FindAsync([serverId], ct)
            ?? throw new KeyNotFoundException("Server not found");
        var pw = encSvc.Decrypt(server.SshPasswordEnc);
        await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw,
            "nginx -t && systemctl reload nginx", ct);
    }

    public async Task DeployConfigAsync(string serverId, string siteName,
        string templateType, string content, CancellationToken ct)
    {
        var server = await db.Servers.FindAsync([serverId], ct)
            ?? throw new KeyNotFoundException("Server not found");
        var pw = encSvc.Decrypt(server.SshPasswordEnc);

        // Write config to remote
        var remotePath = $"/etc/nginx/sites-available/{siteName}";
        var escapedContent = content.Replace("'", "'\\''");
        await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw,
            $"echo '{escapedContent}' > {remotePath} && " +
            $"ln -sf {remotePath} /etc/nginx/sites-enabled/{siteName} && " +
            $"nginx -t && systemctl reload nginx", ct);

        // Save snapshot
        var existing = await db.NginxConfigs
            .FirstOrDefaultAsync(n => n.ServerId == serverId && n.SiteName == siteName, ct);
        if (existing is null)
        {
            db.NginxConfigs.Add(new NginxConfig
            {
                ServerId = serverId, SiteName = siteName,
                TemplateType = templateType, ConfigContent = content,
                LastDeployedAt = DateTime.UtcNow.ToString("O")
            });
        }
        else
        {
            existing.ConfigContent = content;
            existing.LastDeployedAt = DateTime.UtcNow.ToString("O");
        }
        await db.SaveChangesAsync(ct);
    }
}
