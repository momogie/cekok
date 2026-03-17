using Renci.SshNet;

namespace Cekok.Api.Services;

public class SshService
{
    /// <summary>
    /// Run a command on a remote server via SSH, return stdout.
    /// </summary>
    public async Task<string> RunCommandAsync(string host, int port, string user, string password,
        string command, CancellationToken ct = default)
    {
        using var client = new SshClient(host, port, user, password);
        await Task.Run(() => client.Connect(), ct);
        using var cmd = client.RunCommand(command);
        var result = cmd.Result;
        client.Disconnect();
        return result;
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
