using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Cekok.Api.Models;

namespace Cekok.Api.Services;

public class BuildService(IConfiguration config, ILogger<BuildService> logger)
{
    private readonly string _buildDir = config["Cekok:BuildDir"] ?? 
        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Path.Combine(Path.GetTempPath(), "cekok-builds") : "/tmp/cekok-builds");

    public async Task<string> BuildAsync(Application app, string jobId,
        Action<string, string> logCallback, CancellationToken ct)
    {
        // Check free disk
        var freeMb = GetFreeDiskMb(_buildDir);
        var minFree = config.GetValue<long>("Cekok:MinFreeDiskMb", 500);
        if (freeMb < minFree)
            throw new Exception($"Insufficient disk space: {freeMb}MB free, need {minFree}MB");

        var workDir = Path.Combine(_buildDir, app.Id, jobId);
        Directory.CreateDirectory(workDir);

        var repoDir = Path.Combine(workDir, "repo");

        // [2] Git
        if (Directory.Exists(repoDir))
        {
            logCallback("cmd", $"$ git pull origin {app.Branch}");
            await RunProcessAsync("git", $"pull origin {app.Branch}", repoDir, logCallback, ct);
        }
        else
        {
            logCallback("cmd", $"$ git clone {app.RepoUrl} repo");
            await RunProcessAsync("git", $"clone {app.RepoUrl} repo --branch {app.Branch} --depth 1",
                workDir, logCallback, ct);
        }

        // [3] Build
        if (!string.IsNullOrEmpty(app.BuildCmd))
        {
            logCallback("cmd", $"$ {app.BuildCmd}");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await RunProcessAsync("cmd.exe", $"/c \"{app.BuildCmd}\"", repoDir, logCallback, ct);
            }
            else
            {
                await RunProcessAsync("/bin/sh", $"-c \"{app.BuildCmd}\"", repoDir, logCallback, ct);
            }
        }

        // [4] Validate output
        var outputPath = Path.Combine(repoDir, app.OutputDir ?? ".");
        if (!Directory.Exists(outputPath) || !Directory.EnumerateFileSystemEntries(outputPath).Any())
            throw new Exception($"Build output dir is empty: {outputPath}");

        logCallback("success", $"✓ Build complete. Output: {outputPath}");
        return outputPath;
    }

    public void Cleanup(string appId, string jobId)
    {
        var dir = Path.Combine(_buildDir, appId, jobId);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
            logger.LogInformation("Cleaned up build dir: {Dir}", dir);
        }
    }

    private static async Task RunProcessAsync(string exe, string args, string workDir,
        Action<string, string> logCallback, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi) ?? throw new Exception($"Failed to start {exe}");
        p.OutputDataReceived += (_, e) => { if (e.Data != null) logCallback("info", e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) logCallback("warn", e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0) throw new Exception($"Process exited with code {p.ExitCode}");
    }

    private static long GetFreeDiskMb(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var info = new DriveInfo(Path.GetPathRoot(path)!);
            return info.AvailableFreeSpace / (1024 * 1024);
        }
        catch { return long.MaxValue; }
    }
}
