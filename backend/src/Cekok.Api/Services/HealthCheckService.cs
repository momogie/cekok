namespace Cekok.Api.Services;

public class HealthCheckService(IHttpClientFactory factory)
{
    public async Task<bool> CheckAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var client = factory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var resp = await client.GetAsync(url, ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
