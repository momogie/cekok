using Cekok.Api.Data;
using Cekok.Api.Models;
using Cekok.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class ServersController
{
    public static RouteGroupBuilder MapServersEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (CekokDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role == "admin")
                return Results.Ok(await db.Servers.ToListAsync(ct));

            var accessibleIds = await db.UserServerAccesses
                .Where(a => a.UserId == userId)
                .Select(a => a.ServerId)
                .ToListAsync(ct);
            var servers = await db.Servers.Where(s => accessibleIds.Contains(s.Id)).ToListAsync(ct);
            return Results.Ok(servers);
        });

        group.MapPost("/", [Authorize(Roles = "admin")] async (
            CreateServerDto dto, CekokDbContext db, EncryptionService enc, CancellationToken ct) =>
        {
            var server = new Server
            {
                Name = dto.Name, Ip = dto.Ip, SshPort = dto.SshPort,
                SshUser = dto.SshUser,
                SshPasswordEnc = enc.Encrypt(dto.SshPassword),
                Role = dto.Role, Tags = dto.Tags,
            };
            db.Servers.Add(server);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/servers/{server.Id}", server);
        });

        group.MapPut("/{id}", [Authorize(Roles = "admin")] async (
            string id, UpdateServerDto dto, CekokDbContext db, EncryptionService enc, CancellationToken ct) =>
        {
            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();

            srv.Name = dto.Name;
            srv.Ip = dto.Ip;
            srv.SshPort = dto.SshPort;
            srv.SshUser = dto.SshUser;
            srv.Role = dto.Role;
            srv.Tags = dto.Tags;

            if (!string.IsNullOrEmpty(dto.SshPassword))
            {
                srv.SshPasswordEnc = enc.Encrypt(dto.SshPassword);
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id}", [Authorize(Roles = "admin")] async (
            string id, CekokDbContext db, CancellationToken ct) =>
        {
            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            db.Servers.Remove(srv);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapPost("/{id}/test-connection", async (
            string id, CekokDbContext db, SshService ssh, EncryptionService enc, CancellationToken ct) =>
        {
            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            var pw = enc.Decrypt(srv.SshPasswordEnc);
            var ok = await ssh.TestConnectionAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, ct);
            
            if (ok)
            {
                try {
                    var hostname = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "hostname", ct)).Trim();
                    if (srv.Hostname != hostname)
                    {
                        srv.Hostname = hostname;
                        await db.SaveChangesAsync(ct);
                    }
                } catch { /* ignore if hostname fetch fails but connection was ok */ }
            }

            return Results.Ok(new { connected = ok, hostname = srv.Hostname });
        });

        group.MapGet("/{id}/sys-info", async (
            string id, CekokDbContext db, HttpContext ctx, SshService ssh, EncryptionService enc, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role != "admin")
            {
                var access = await db.UserServerAccesses.AnyAsync(a => a.UserId == userId && a.ServerId == id, ct);
                if (!access) return Results.Forbid();
            }

            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            var pw = enc.Decrypt(srv.SshPasswordEnc);

            try {
                var hostname = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "hostname", ct)).Trim();
                
                if (srv.Hostname != hostname)
                {
                    srv.Hostname = hostname;
                    await db.SaveChangesAsync(ct);
                }

                var cpuModel = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "grep 'model name' /proc/cpuinfo | head -1 | cut -d: -f2 | xargs", ct)).Trim();
                var cpuCount = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "nproc", ct)).Trim();
                var uptime = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "uptime -p", ct)).Trim();
                
                return Results.Ok(new {
                    hostname,
                    uptime,
                    cpu = $"{cpuCount} Cores ({cpuModel})",
                    cpuCores = int.TryParse(cpuCount, out var cc) ? cc : 0,
                });
            } catch (Exception ex) {
                return Results.Problem(ex.Message);
            }
        });

        group.MapGet("/{id}/stats", async (
            string id, CekokDbContext db, HttpContext ctx, SshService ssh, EncryptionService enc, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role != "admin")
            {
                var access = await db.UserServerAccesses.AnyAsync(a => a.UserId == userId && a.ServerId == id, ct);
                if (!access) return Results.Forbid();
            }

            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            var pw = enc.Decrypt(srv.SshPasswordEnc);

            try {
                var cpuUsage = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "top -bn1 | grep 'Cpu(s)' | awk '{print $2+$4}'", ct)).Trim();
                var ramData = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "free -m | awk '/^Mem:/ {print $3, $2}'", ct)).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var swapData = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "free -m | awk '/^Swap:/ {print $3, $2}'", ct)).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var diskRaw = await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "df -m -x tmpfs -x devtmpfs -x squashfs -x overlay --output=source,size,used,avail,pcent,target | tail -n +2", ct);
                var disks = diskRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return new {
                        FileSystem = parts.ElementAtOrDefault(0),
                        Total = long.TryParse(parts.ElementAtOrDefault(1), out var t) ? t : 0,
                        Used = long.TryParse(parts.ElementAtOrDefault(2), out var u) ? u : 0,
                        Free = long.TryParse(parts.ElementAtOrDefault(3), out var f) ? f : 0,
                        Percent = parts.ElementAtOrDefault(4),
                        Mount = parts.ElementAtOrDefault(5)
                    };
                }).Where(d => d.FileSystem != null && d.FileSystem.StartsWith("/dev/")).ToList();
                var netData = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "awk 'NR>2 && $1 != \"lo:\" {print $2,$10; exit}' /proc/net/dev", ct)).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var uptime = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "uptime -p", ct)).Trim();

                return Results.Ok(new {
                    uptime,
                    cpuUsage = double.TryParse(cpuUsage, out var c) ? c : 0,
                    ramUsed = int.TryParse(ramData.ElementAtOrDefault(0), out var ru) ? ru : 0,
                    ramTotal = int.TryParse(ramData.ElementAtOrDefault(1), out var rt) ? rt : 0,
                    swapUsed = int.TryParse(swapData.ElementAtOrDefault(0), out var su) ? su : 0,
                    swapTotal = int.TryParse(swapData.ElementAtOrDefault(1), out var st) ? st : 0,
                    disks,
                    diskUsed = disks.FirstOrDefault(d => d.Mount == "/")?.Used ?? 0,
                    diskTotal = disks.FirstOrDefault(d => d.Mount == "/")?.Total ?? 0,
                    netRx = long.TryParse(netData.ElementAtOrDefault(0), out var nrx) ? nrx : 0,
                    netTx = long.TryParse(netData.ElementAtOrDefault(1), out var ntx) ? ntx : 0
                });
            } catch (Exception ex) {
                return Results.Problem(ex.Message);
            }
        });

        group.MapGet("/{id}/network", async (
            string id, CekokDbContext db, HttpContext ctx, SshService ssh, EncryptionService enc, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role != "admin")
            {
                var access = await db.UserServerAccesses.AnyAsync(a => a.UserId == userId && a.ServerId == id, ct);
                if (!access) return Results.Forbid();
            }

            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            var pw = enc.Decrypt(srv.SshPasswordEnc);
            var sudoPrefix = srv.SshUser == "root" ? "" : $"echo '{pw.Replace("'", "'\\''")}' | sudo -S ";

            try {
                var ifaceRaw = await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "ip -o addr show | awk '{print $2,$4}'", ct);
                var pingStatus = await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "cat /proc/net/dev | awk 'NR>2 {print $1,$2,$10}'", ct);
                
                var fwPortsStr = "";
                var fwType = "none";
                var fwStatus = "inactive";

                // 1. Check firewalld
                try {
                    var fwState = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, $"{sudoPrefix}firewall-cmd --state 2>/dev/null", ct)).Trim();
                    if (fwState == "running" || fwState == "not running")
                    {
                        fwType = "firewalld";
                        fwStatus = fwState == "running" ? "active" : "inactive";
                        if (fwStatus == "active")
                        {
                            var fwp = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, $"{sudoPrefix}firewall-cmd --list-ports 2>/dev/null", ct)).Trim();
                            var fws = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, $"{sudoPrefix}firewall-cmd --list-services 2>/dev/null", ct)).Trim();
                            fwPortsStr = (fwp + " " + fws).Trim();
                        }
                    }
                } catch { }

                // 2. Check UFW (priority if firewalld is inactive or missing)
                if (fwStatus != "active")
                {
                    try {
                        var ufwOutput = (await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, $"{sudoPrefix}ufw status 2>/dev/null", ct)).Trim();
                        if (ufwOutput.Contains("Status:", StringComparison.OrdinalIgnoreCase))
                        {
                            var currentStatus = ufwOutput.Contains("active", StringComparison.OrdinalIgnoreCase) ? "active" : "inactive";
                            if (currentStatus == "active" || fwType == "none")
                            {
                                fwType = "ufw";
                                fwStatus = currentStatus;
                                if (fwStatus == "active")
                                {
                                    var ufwLines = ufwOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                        .Where(l => l.Contains("ALLOW", StringComparison.OrdinalIgnoreCase))
                                        .Select(l => l.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
                                        .Where(p => !string.IsNullOrEmpty(p) && p.ToLower() != "to")
                                        .Distinct();
                                    fwPortsStr = string.Join(" ", ufwLines);
                                }
                            }
                        }
                    } catch { }
                }

                var trafficMap = pingStatus.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToDictionary(
                    line => line.Split(':', StringSplitOptions.RemoveEmptyEntries)[0].Trim(),
                    line => {
                        var parts = line.Split(':', 2).LastOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return new {
                            Rx = long.TryParse(parts?.ElementAtOrDefault(0), out var rx) ? rx : 0,
                            Tx = long.TryParse(parts?.ElementAtOrDefault(1), out var tx) ? tx : 0
                        };
                    });

                var interfaces = ifaceRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return new { Name = parts.ElementAtOrDefault(0), Address = parts.ElementAtOrDefault(1) };
                }).GroupBy(x => x.Name).Select(g => new { 
                    Name = g.Key, 
                    Addresses = g.Select(x => x.Address).ToList(),
                    Rx = trafficMap.TryGetValue(g.Key ?? "", out var t) ? t.Rx : 0,
                    Tx = trafficMap.TryGetValue(g.Key ?? "", out var t2) ? t2.Tx : 0
                }).ToList();

                var portsRaw = await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "ss -tuln | awk 'NR>1 {print $1,$5}'", ct);
                var ports = portsRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var fullAddr = parts.ElementAtOrDefault(1) ?? "";
                    var port = fullAddr.Split(':').LastOrDefault();
                    return new { Proto = parts.ElementAtOrDefault(0), Port = port, Address = fullAddr };
                }).Distinct().OrderBy(p => int.TryParse(p.Port, out var val) ? val : 99999).ToList();

                var fwPorts = fwPortsStr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                return Results.Ok(new { interfaces, ports, fwPorts, fwType, fwStatus });
            } catch (Exception ex) {
                return Results.Problem(ex.Message);
            }
        });

        group.MapGet("/{id}/processes", async (
            string id, CekokDbContext db, HttpContext ctx, SshService ssh, EncryptionService enc, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role != "admin")
            {
                var access = await db.UserServerAccesses.AnyAsync(a => a.UserId == userId && a.ServerId == id, ct);
                if (!access) return Results.Forbid();
            }

            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            var pw = enc.Decrypt(srv.SshPasswordEnc);

            try {
                var procRaw = await ssh.RunCommandAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, "ps -eo pid,ppid,cmd,%cpu,%mem --sort=-%cpu | head -n 11 | tail -n +2", ct);
                var processes = procRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => {
                    var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return new {
                        Pid = parts.ElementAtOrDefault(0),
                        Ppid = parts.ElementAtOrDefault(1),
                        Cpu = parts.ElementAtOrDefault(parts.Length - 2),
                        Mem = parts.ElementAtOrDefault(parts.Length - 1),
                        Command = string.Join(" ", parts.Skip(2).Take(parts.Length - 4))
                    };
                }).ToList();

                return Results.Ok(processes);
            } catch (Exception ex) {
                return Results.Problem(ex.Message);
            }
        });

        group.MapPost("/{id}/execute", async (
            string id, ExecuteCmdDto dto, CekokDbContext db, HttpContext ctx, SshService ssh, EncryptionService enc, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role != "admin")
            {
                var access = await db.UserServerAccesses.FirstOrDefaultAsync(a => a.UserId == userId && a.ServerId == id, ct);
                if (access == null || !access.CanManage) return Results.Forbid();
            }

            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            var pw = enc.Decrypt(srv.SshPasswordEnc);

            try {
                var (output, error, status) = await ssh.RunCommandDetailedAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, dto.Command, ct);
                return Results.Ok(new { output, error, status });
            } catch (Exception ex) {
                return Results.Problem(ex.Message);
            }
        });

        group.MapPost("/{id}/execute-stream", async (
            string id, ExecuteCmdDto dto, CekokDbContext db, HttpContext ctx, SshService ssh, EncryptionService enc, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role != "admin")
            {
                var access = await db.UserServerAccesses.FirstOrDefaultAsync(a => a.UserId == userId && a.ServerId == id, ct);
                if (access == null || !access.CanManage) return Results.Forbid();
            }

            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            var pw = enc.Decrypt(srv.SshPasswordEnc);

            return Results.Stream(async stream => {
                using var writer = new StreamWriter(stream);
                await foreach (var line in ssh.RunCommandStreamAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, dto.Command, ct))
                {
                    await writer.WriteLineAsync(line);
                    await writer.FlushAsync();
                }
            }, "text/plain");
        });

        return group;
    }

    public record CreateServerDto(string Name, string Ip, int SshPort, string SshUser,
        string SshPassword, string Role, string? Tags);

    public record UpdateServerDto(string Name, string Ip, int SshPort, string SshUser,
        string? SshPassword, string Role, string? Tags);

    public record ExecuteCmdDto(string Command);
}
