using Renci.SshNet;

namespace Cekok.Api.Services;

public class ScpService
{
    /// <summary>
    /// Upload directory contents to remote server via SCP.
    /// </summary>
    public async Task UploadDirectoryAsync(string host, int port, string user, string password,
        string localDir, string remoteDir, IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        using var client = new ScpClient(host, port, user, password);
        await Task.Run(() =>
        {
            client.Connect();
            client.Uploading += (sender, e) =>
            {
                if (e.Size > 0)
                    progress?.Report((int)(e.Uploaded * 100 / e.Size));
            };
            var dir = new DirectoryInfo(localDir);
            client.Upload(dir, remoteDir);
            client.Disconnect();
        }, ct);
    }
}
