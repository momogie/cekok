using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

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
        };

        var statuses = new Dictionary<string, string>();

        // Static apps
        foreach (var app in apps)
        {
            try
            {
                var result = await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw, app.Value, ct);
                statuses[app.Key] = result.Trim() == "active" ? "active" : "inactive";
            }
            catch { statuses[app.Key] = "inactive"; }
        }

        // Handle .NET versions
        try
        {
            var sdkOutput = await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw, "dotnet --list-sdks", ct);
            var runtimeOutput = await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw, "dotnet --list-runtimes", ct);

            // Logic to mark specific versions as active
            // IDs planned: dotnet-sdk-8.0, dotnet-sdk-10.0, dotnet-runtime-8.0, dotnet-runtime-10.0, node
            statuses["dotnet-sdk-8.0"] = sdkOutput.Contains("8.0.") ? "active" : "inactive";
            statuses["dotnet-sdk-10.0"] = sdkOutput.Contains("10.0.") ? "active" : "inactive";
            statuses["dotnet-runtime-8.0"] = runtimeOutput.Contains("Microsoft.AspNetCore.App 8.0.") ? "active" : "inactive";
            statuses["dotnet-runtime-10.0"] = runtimeOutput.Contains("Microsoft.AspNetCore.App 10.0.") ? "active" : "inactive";
            
            // Legacy/Summary status
            statuses["dotnet"] = (statuses["dotnet-sdk-8.0"] == "active" || statuses["dotnet-sdk-10.0"] == "active") ? "active" : "inactive";
        }
        catch { 
            statuses["dotnet-sdk-8.0"] = "inactive";
            statuses["dotnet-sdk-10.0"] = "inactive";
            statuses["dotnet-runtime-8.0"] = "inactive";
            statuses["dotnet-runtime-10.0"] = "inactive";
        }

        // Node
        try {
            var nodeRes = await sshSvc.RunCommandAsync(server.Ip, server.SshPort, server.SshUser, pw, "node --version", ct);
            var trimmed = nodeRes.Trim().TrimStart('v');
            statuses["node"] = !string.IsNullOrWhiteSpace(trimmed) && char.IsDigit(trimmed[0]) ? "active" : "inactive";
        } catch { statuses["node"] = "inactive"; }

        return statuses;
    }

    public async IAsyncEnumerable<string> InstallStreamAsync(string serverId, string appId, [EnumeratorCancellation] CancellationToken ct)
    {
        var server = await db.Servers.FindAsync([serverId], ct)
            ?? throw new KeyNotFoundException("Server not found");
        var pw = encSvc.Decrypt(server.SshPasswordEnc);

        var sudoPrefix = server.SshUser == "root" ? "" : $"echo '{pw.Replace("'", "'\\''")}' | sudo -S ";
        
        string command = appId.ToLower() switch
        {
            "nginx" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing nginx && systemctl enable --now nginx'",
            "redis" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing redis-server && systemctl enable --now redis-server'",
            "dotnet-sdk-8.0" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true wget ca-certificates || true; . /etc/os-release; wget -q \"https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb\" -O prod.deb || wget -q \"https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb\" -O prod.deb; if [ -f prod.deb ]; then dpkg -i --force-confold --force-confdef prod.deb && rm prod.deb && apt-get update -qq -o Acquire::ForceIPv4=true || true; fi; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing dotnet-sdk-8.0'",
            "dotnet-sdk-10.0" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true wget ca-certificates || true; . /etc/os-release; wget -q \"https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb\" -O prod.deb || wget -q \"https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb\" -O prod.deb; if [ -f prod.deb ]; then dpkg -i --force-confold --force-confdef prod.deb && rm prod.deb && apt-get update -qq -o Acquire::ForceIPv4=true || true; fi; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing dotnet-sdk-10.0'",
            "dotnet-runtime-8.0" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true wget ca-certificates || true; . /etc/os-release; wget -q \"https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb\" -O prod.deb || wget -q \"https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb\" -O prod.deb; if [ -f prod.deb ]; then dpkg -i --force-confold --force-confdef prod.deb && rm prod.deb && apt-get update -qq -o Acquire::ForceIPv4=true || true; fi; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing aspnetcore-runtime-8.0'",
            "dotnet-runtime-10.0" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq -o Acquire::ForceIPv4=true || true; apt-get install -y -o Acquire::ForceIPv4=true wget ca-certificates || true; . /etc/os-release; wget -q \"https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb\" -O prod.deb || wget -q \"https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb\" -O prod.deb; if [ -f prod.deb ]; then dpkg -i --force-confold --force-confdef prod.deb && rm prod.deb && apt-get update -qq -o Acquire::ForceIPv4=true || true; fi; apt-get install -y -o Acquire::ForceIPv4=true -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" --fix-missing aspnetcore-runtime-10.0'",
            "dotnet" => $"{sudoPrefix}bash -c 'apt-get install -y dotnet-sdk-10.0'",
            "node" => $"{sudoPrefix}bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update -qq || true; apt-get install -y curl || true; curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && apt-get install -y -o Dpkg::Options::=\"--force-confold\" -o Dpkg::Options::=\"--force-confdef\" nodejs'",
            _ => throw new ArgumentException("Invalid App ID")
        };

        using var client = new Renci.SshNet.SshClient(server.Ip, server.SshPort, server.SshUser, pw);
        await Task.Run(() => client.Connect(), ct);

        using var cmd = client.CreateCommand(command);
        var result = cmd.BeginExecute();

        using var reader = new StreamReader(cmd.OutputStream);
        using var errorReader = new StreamReader(cmd.ExtendedOutputStream);

        int exitStatus = -1;
        while (!result.IsCompleted || !reader.EndOfStream || !errorReader.EndOfStream)
        {
            if (ct.IsCancellationRequested) break;

            bool hadOutput = false;

            if (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line != null) { yield return line; hadOutput = true; }
            }

            if (!errorReader.EndOfStream)
            {
                var line = await errorReader.ReadLineAsync();
                if (line != null) { yield return "ERROR: " + line; hadOutput = true; }
            }

            if (!hadOutput && !result.IsCompleted)
            {
                await Task.Delay(50, ct);
            }

            if (result.IsCompleted && reader.EndOfStream && errorReader.EndOfStream)
            {
                exitStatus = cmd.ExitStatus ?? -1;
                break;
            }
        }

        client.Disconnect();
        
        yield return $"\n[EXIT_STATUS]: {exitStatus}";
        
        if (exitStatus == 0)
        {
            if (appId.ToLower() == "nginx")
            {
                server.NginxInstalled = true;
                await db.SaveChangesAsync(ct);
            }
        }
    }
}

