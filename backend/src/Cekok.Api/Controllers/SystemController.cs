using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class SystemController
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder group)
    {
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
