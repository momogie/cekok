using Cekok.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class SystemAppsController
{
    public static RouteGroupBuilder MapSystemAppsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{serverId}/status", async (
            string serverId, SystemAppService svc, CancellationToken ct) =>
        {
            var statuses = await svc.GetAllStatusesAsync(serverId, ct);
            return Results.Ok(statuses);
        });

        group.MapPost("/{serverId}/install/{appId}", [Authorize(Roles = "admin")] async (
            string serverId, string appId, SystemAppService svc, HttpContext context, CancellationToken ct) =>
        {
            context.Response.ContentType = "text/plain";
            await foreach (var line in svc.InstallStreamAsync(serverId, appId, ct))
            {
                await context.Response.WriteAsync(line + "\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
        });

        return group;
    }
}
