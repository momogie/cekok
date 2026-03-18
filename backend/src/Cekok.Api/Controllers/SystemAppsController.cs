using Cekok.Api.Data;
using Cekok.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cekok.Api.Controllers;

public static class SystemAppsController
{
    public static RouteGroupBuilder MapSystemAppsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{serverId}/status", async (
            string serverId, SystemAppService svc, CekokDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role != "admin")
            {
                var access = await db.UserServerAccesses.AnyAsync(a => a.UserId == userId && a.ServerId == serverId, ct);
                if (!access) return Results.Forbid();
            }

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
