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
            { "dotnet", "dotnet --version" },
            { "node", "node --version" }
        };

        var statuses = new Dictionary<string, string>();

        foreach (var app in apps)
        {
            try
            {
                var result = await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw, app.Value, ct);
                if (app.Key == "dotnet" || app.Key == "node")
                {
                    // For dotnet and node, we check if the result starts with a digit/v (e.g., 8.0.x or v20.x)
                    var trimmed = result.Trim().TrimStart('v');
                    statuses[app.Key] = !string.IsNullOrWhiteSpace(trimmed) && char.IsDigit(trimmed[0]) ? "active" : "inactive";
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

        var sudoPrefix = server.SshUser == "root" ? "" : $"echo '{pw.Replace("'", "'\\''")}' | sudo -S ";
        
        string command = appId.ToLower() switch
        {
            "nginx" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing nginx && systemctl enable --now nginx'",
            "redis" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing redis-server && systemctl enable --now redis-server'",
            "dotnet" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true wget ca-certificates || true; . /etc/os-release; wget -q \"https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb\" -O prod.deb || wget -q \"https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb\" -O prod.deb || wget -q \"https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb\" -O prod.deb; if [ -f prod.deb ]; then dpkg -i --force-confold --force-confdef prod.deb && rm prod.deb && apt-get update -qq -o Acquire::ForceIPv4=true || true; fi; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing dotnet-sdk-8.0 || apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing dotnet8'",
            "node" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq || true; apt-get install -y curl || true; curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && apt-get install -y -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" nodejs'",
            _ => throw new ArgumentException("Invalid App ID")
        };

        var result = await sshSvc.RunCommandDetailedAsync(server.Ip, server.SshPort, server.SshUser, pw, command, ct);

        if (result.ExitStatus == 0)
        {
            if (appId.ToLower() == "nginx")
            {
                server.NginxInstalled = true;
                await db.SaveChangesAsync(ct);
            }
            // Optional: add more flags to Server model if updated in future
        }

        return result;
    }
}

