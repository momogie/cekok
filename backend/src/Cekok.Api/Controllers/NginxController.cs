using Cekok.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class NginxController
{
    public static RouteGroupBuilder MapNginxEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{serverId}/status", async (
            string serverId, NginxService svc, CancellationToken ct) =>
        {
            var status = await svc.GetStatusAsync(serverId, ct);
            return Results.Ok(new { status });
        });

        group.MapPost("/{serverId}/install", [Authorize(Roles = "admin")] async (
            string serverId, NginxService svc, CancellationToken ct) =>
        {
            await svc.InstallAsync(serverId, ct);
            return Results.Ok(new { message = "nginx installed and enabled" });
        });

        group.MapPost("/{serverId}/reload", async (
            string serverId, NginxService svc, CancellationToken ct) =>
        {
            await svc.ReloadAsync(serverId, ct);
            return Results.Ok(new { message = "nginx reloaded" });
        });

        group.MapPost("/{serverId}/deploy-config", async (
            string serverId, DeployConfigDto dto, NginxService svc, CancellationToken ct) =>
        {
            await svc.DeployConfigAsync(serverId, dto.SiteName, dto.TemplateType, dto.Content, ct);
            return Results.Ok(new { message = "config deployed and nginx reloaded" });
        });

        return group;
    }

    public record DeployConfigDto(string SiteName, string TemplateType, string Content);
}
