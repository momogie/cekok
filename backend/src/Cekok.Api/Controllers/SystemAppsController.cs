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
            string serverId, string appId, SystemAppService svc, CancellationToken ct) =>
        {
            var res = await svc.InstallAsync(serverId, appId, ct);
            return Results.Ok(new { 
                success = res.ExitStatus == 0,
                message = res.ExitStatus == 0 ? $"{appId} installed successfully" : $"{appId} installation failed",
                output = res.Output,
                error = res.Error,
                exitStatus = res.ExitStatus
            });
        });

        return group;
    }
}
