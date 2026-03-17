using Renci.SshNet;
using System.Runtime.CompilerServices;

namespace Cekok.Api.Services;

public class SshService
{
    public async Task<(string Output, string Error, int ExitStatus)> RunCommandDetailedAsync(string host, int port, string user, string password,
        string command, CancellationToken ct = default)
    {
        using var client = new SshClient(host, port, user, password);
        await Task.Run(() => client.Connect(), ct);
        using var cmd = client.RunCommand(command);
        var output = cmd.Result;
        var error = cmd.Error;
        var exitStatus = cmd.ExitStatus;
        client.Disconnect();
        return (output, error, exitStatus ?? -1);
    }

    /// <summary>
    /// Run a command on a remote server via SSH, return stdout.
    /// </summary>
    public async Task<string> RunCommandAsync(string host, int port, string user, string password,
        string command, CancellationToken ct = default)
    {
        var res = await RunCommandDetailedAsync(host, port, user, password, command, ct);
        return res.Output;
    }

    public async IAsyncEnumerable<string> RunCommandStreamAsync(string host, int port, string user, string password,
        string command, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var client = new SshClient(host, port, user, password);
        await Task.Run(() => client.Connect(), ct);

        using var cmd = client.CreateCommand(command);
        var result = cmd.BeginExecute();

        using var reader = new StreamReader(cmd.OutputStream);
        using var errorReader = new StreamReader(cmd.ExtendedOutputStream);

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

            if (result.IsCompleted && reader.EndOfStream && errorReader.EndOfStream) break;
        }

        client.Disconnect();
    }

    /// <summary>
    /// Test SSH connectivity. Returns true if connected successfully.
    /// </summary>
    public async Task<bool> TestConnectionAsync(string host, int port, string user, string password,
        CancellationToken ct = default)
    {
        try
        {
            using var client = new SshClient(host, port, user, password);
            await Task.Run(() => client.Connect(), ct);
            var ok = client.IsConnected;
            client.Disconnect();
            return ok;
        }
        catch { return false; }
    }
}
