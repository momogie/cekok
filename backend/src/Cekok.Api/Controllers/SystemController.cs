using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Cekok.Api.Data;
using Cekok.Api.Models;
using Cekok.Api.Services;

namespace Cekok.Api.Controllers;

public static class SystemController
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/resources", async (CancellationToken ct) =>
        {
            double cpuUsage = 0;
            double ramUsedGb = 0;
            double ramTotalGb = 0;
            string cpuName = "Unknown";
            string hostname = Environment.MachineName;
            int vcpuCount = Environment.ProcessorCount;
            
            var uptimeSpan = TimeSpan.FromMilliseconds(Environment.TickCount64);
            string uptime = $"{(int)uptimeSpan.TotalDays}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var cpu = await RunCommandAsync("wmic", "cpu get loadpercentage /Value", ct);
                if (cpu.ExitCode == 0 && cpu.Output.Contains("LoadPercentage="))
                {
                    var valStr = cpu.Output.Split("LoadPercentage=")[1].Split('\n')[0].Trim();
                    if (double.TryParse(valStr, out var v)) cpuUsage = v;
                }

                var cpuNameRes = await RunCommandAsync("wmic", "cpu get name /Value", ct);
                if (cpuNameRes.ExitCode == 0 && cpuNameRes.Output.Contains("Name="))
                {
                    cpuName = cpuNameRes.Output.Split("Name=")[1].Split('\n')[0].Trim();
                }

                var mem = await RunCommandAsync("wmic", "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value", ct);
                if (mem.ExitCode == 0)
                {
                    double free = 0; double total = 0;
                    foreach (var rawLine in mem.Output.Split('\n'))
                    {
                        var line = rawLine.Trim();
                        if (line.StartsWith("FreePhysicalMemory=")) double.TryParse(line.Split('=')[1].Trim(), out free);
                        if (line.StartsWith("TotalVisibleMemorySize=")) double.TryParse(line.Split('=')[1].Trim(), out total);
                    }
                    if (total > 0)
                    {
                        ramTotalGb = total / 1024 / 1024;
                        ramUsedGb = (total - free) / 1024 / 1024;
                    }
                }
            }
            else
            {
                // Linux
                var cpu = await RunCommandAsync("bash", "-c \"top -bn1 | grep 'Cpu(s)' | awk '{print $2+$4}'\"", ct);
                if (cpu.ExitCode == 0 && double.TryParse(cpu.Output.Trim(), out var c)) cpuUsage = c;

                var cpuNameRes = await RunCommandAsync("bash", "-c \"grep 'model name' /proc/cpuinfo | head -n 1 | awk -F': ' '{print $2}'\"", ct);
                if (cpuNameRes.ExitCode == 0) cpuName = cpuNameRes.Output.Trim();

                var mem = await RunCommandAsync("bash", "-c \"free -m | awk '/^Mem:/ {print $3, $2}'\"", ct);
                if (mem.ExitCode == 0)
                {
                    var parts = mem.Output.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && double.TryParse(parts[0], out var u) && double.TryParse(parts[1], out var t))
                    {
                        ramUsedGb = u / 1024;
                        ramTotalGb = t / 1024;
                    }
                }
            }

            // Disk Usage
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);
            double diskTotalGb = 0;
            double diskUsedGb = 0;
            if (drive != null)
            {
                diskTotalGb = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
                diskUsedGb = (drive.TotalSize - drive.AvailableFreeSpace) / 1024.0 / 1024.0 / 1024.0;
            }

            return Results.Ok(new {
                cpuUsage = Math.Round(cpuUsage, 1),
                ramUsed = Math.Round(ramUsedGb, 1),
                ramTotal = Math.Round(ramTotalGb, 1),
                diskUsed = Math.Round(diskUsedGb, 1),
                diskTotal = Math.Round(diskTotalGb, 1),
                hostname,
                cpuName,
                vcpuCount,
                uptime,
                osVersion = RuntimeInformation.OSDescription
            });
        });

        group.MapGet("/env-checks", async (CancellationToken ct) =>
        {
            var git = await RunCommandAsync("git", "--version", ct);
            var node = await RunCommandAsync("node", "--version", ct);
            var dotnet = await RunCommandAsync("dotnet", "--list-sdks", ct);
            var ssh = await RunCommandAsync("ssh", "-V", ct);

            return Results.Ok(new {
                git = new {
                    installed = git.ExitCode == 0,
                    version = git.ExitCode == 0 ? git.Output.Trim() : null
                },
                node = new {
                    installed = node.ExitCode == 0,
                    version = node.ExitCode == 0 ? node.Output.Trim() : null
                },
                dotnet = new {
                    installed = dotnet.ExitCode == 0,
                    version = dotnet.ExitCode == 0 ? dotnet.Output.Trim() : null,
                    hasSdk8 = dotnet.ExitCode == 0 && dotnet.Output.Contains("8.0."),
                    hasSdk10 = dotnet.ExitCode == 0 && dotnet.Output.Contains("10.0.")
                },
                ssh = new {
                    installed = ssh.ExitCode == 0,
                    version = ssh.ExitCode == 0 ? (ssh.Error.Contains("OpenSSH") ? ssh.Error.Trim() : ssh.Output.Trim()) : null
                }
            });
        });

        group.MapGet("/settings", [Authorize(Roles = "admin")] async (CekokDbContext db, CancellationToken ct) =>
        {
            var settings = await db.SystemSettings.ToListAsync(ct);
            return Results.Ok(settings.Select(s => new {
                s.Key,
                Value = s.IsSecure ? "********" : s.Value,
                s.Group,
                s.IsSecure,
                s.UpdatedAt
            }));
        });

        group.MapPost("/settings", [Authorize(Roles = "admin")] async (List<UpdateSettingRequest> req, CekokDbContext db, EncryptionService encryption, CancellationToken ct) =>
        {
            foreach (var item in req)
            {
                var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == item.Key, ct);
                if (setting == null)
                {
                    setting = new SystemSetting { 
                        Key = item.Key, 
                        Group = item.Group ?? "general", 
                        IsSecure = item.IsSecure 
                    };
                    db.SystemSettings.Add(setting);
                }

                if (item.IsSecure && item.Value == "********") continue;

                setting.Value = item.IsSecure ? encryption.Encrypt(item.Value) : item.Value;
                setting.IsSecure = item.IsSecure;
                setting.Group = item.Group ?? setting.Group;
                setting.UpdatedAt = DateTime.UtcNow.ToString("O");
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        });

        group.MapGet("/telegram/subscribers", [Authorize(Roles = "admin")] async (CekokDbContext db, CancellationToken ct) =>
        {
            var subs = await db.TelegramSubscribers.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
            return Results.Ok(subs);
        });

        group.MapDelete("/telegram/subscribers/{chatId}", [Authorize(Roles = "admin")] async (string chatId, CekokDbContext db, CancellationToken ct) =>
        {
            var sub = await db.TelegramSubscribers.FirstOrDefaultAsync(s => s.ChatId == chatId, ct);
            if (sub != null)
            {
                db.TelegramSubscribers.Remove(sub);
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        });

        return group;
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCommandAsync(string fileName, string arguments, CancellationToken ct)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            return (process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }
}

public record UpdateSettingRequest(string Key, string Value, string? Group, bool IsSecure);
