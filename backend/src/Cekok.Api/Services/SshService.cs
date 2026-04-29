using Renci.SshNet;
using System.Runtime.CompilerServices;

namespace Cekok.Api.Services;

public class SshService
{
    public static AsyncLocal<string?> CurrentSuPassword { get; } = new();
    public async Task<(string Output, string Error, int ExitStatus)> RunCommandDetailedAsync(string host, int port, string user, string password,
        string command, CancellationToken ct = default)
    {
        using var client = new SshClient(host, port, user, password);
        await Task.Run(() => client.Connect(), ct);
        
        var finalCommand = command;
        var activeSuPassword = CurrentSuPassword.Value;
        if (!string.IsNullOrEmpty(activeSuPassword) && user != "root")
        {
            var escapedSuPw = activeSuPassword.Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$");
            var escapedCmd = command.Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$");
            finalCommand = $"python3 -c \"import sys, pty, os; pid, fd = pty.fork(); os.execvp('su', ['su', '-c', sys.argv[2]]) if pid == 0 else (os.write(fd, sys.argv[1].encode() + b'\\n'), exec('try:\\n skip=True\\n while True:\\n  d=os.read(fd,4096)\\n  if not d:break\\n  if skip:\\n   if b\\\":\\\" in d:\\n    d=d[d.find(b\\\":\\\")+1:].lstrip()\\n    skip=False\\n   else:\\n    continue\\n  sys.stdout.buffer.write(d);sys.stdout.flush()\\nexcept:pass'))\" \"{escapedSuPw}\" \"{escapedCmd}\"";
        }

        using var cmd = client.RunCommand(finalCommand);
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
        var (output, error, exitStatus) = await RunCommandDetailedAsync(host, port, user, password, command, ct);
        if (exitStatus != 0)
        {
            var msg = string.IsNullOrEmpty(error) ? output : error;
            throw new Exception($"SSH command failed with exit code {exitStatus}: {msg}");
        }
        return output;
    }

    public async IAsyncEnumerable<string> RunCommandStreamAsync(string host, int port, string user, string password,
        string command, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var client = new SshClient(host, port, user, password);
        await Task.Run(() => client.Connect(), ct);

        var finalCommand = command;
        var activeSuPassword = CurrentSuPassword.Value;
        if (!string.IsNullOrEmpty(activeSuPassword) && user != "root")
        {
            var escapedSuPw = activeSuPassword.Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$");
            var escapedCmd = command.Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$");
            finalCommand = $"python3 -c \"import sys, pty, os; pid, fd = pty.fork(); os.execvp('su', ['su', '-c', sys.argv[2]]) if pid == 0 else (os.write(fd, sys.argv[1].encode() + b'\\n'), exec('try:\\n skip=True\\n while True:\\n  d=os.read(fd,4096)\\n  if not d:break\\n  if skip:\\n   if b\\\":\\\" in d:\\n    d=d[d.find(b\\\":\\\")+1:].lstrip()\\n    skip=False\\n   else:\\n    continue\\n  sys.stdout.buffer.write(d);sys.stdout.flush()\\nexcept:pass'))\" \"{escapedSuPw}\" \"{escapedCmd}\"";
        }

        using var cmd = client.CreateCommand(finalCommand);
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
