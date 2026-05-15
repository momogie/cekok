namespace Cekok.Api.Services.Abstractions;

public interface IScpService
{
    Task UploadDirectoryAsync(string host, int port, string user, string password, string localDir, string remoteDir, IProgress<int>? progress = null, CancellationToken ct = default);
    Task UploadFileAsync(string host, int port, string user, string password, string localFile, string remoteTarget, IProgress<int>? progress = null, CancellationToken ct = default);
}
