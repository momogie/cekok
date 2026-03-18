using Cekok.Api.Data;
using Cekok.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class DeployController
{
    public static RouteGroupBuilder MapDeployEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{appId}", async (
            string appId, DeployService svc,
            CekokDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var role   = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            // viewer cannot deploy
            if (role == "viewer") return Results.Forbid();

            IEnumerable<string> allowedServerIds;
            if (role == "admin")
            {
                allowedServerIds = await db.Servers.Select(s => s.Id).ToListAsync(ct);
            }
            else
            {
                allowedServerIds = await db.UserServerAccesses
                    .Where(a => a.UserId == userId && a.CanDeploy)
                    .Select(a => a.ServerId)
                    .ToListAsync(ct);
            }

            try
            {
                var job = await svc.TriggerAsync(appId, "manual", userId, allowedServerIds, ct);
                return Results.Accepted($"/api/deploy/{appId}/status", job);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException ex) { return Results.NotFound(ex.Message); }
        });

        group.MapGet("/{appId}/status", async (
            string appId, DeployService svc, CancellationToken ct) =>
        {
            var job = await svc.GetStatusAsync(appId, ct);
            return job is null ? Results.NotFound() : Results.Ok(job);
        });

        group.MapGet("/{appId}/logs", async (
            string appId, string? jobId, DeployService svc,
            CekokDbContext db, CancellationToken ct) =>
        {
            var resolvedJobId = jobId ?? (await svc.GetStatusAsync(appId, ct))?.Id;
            if (resolvedJobId is null) return Results.NotFound();
            return Results.Ok(await svc.GetLogsAsync(resolvedJobId, ct));
        });

        group.MapGet("/history", async (
            int page, int size, DeployService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetHistoryAsync(page, size < 1 ? 20 : size, ct)));

        group.MapGet("/{appId}/history", async (
            string appId, int page, int size, DeployService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAppHistoryAsync(appId, page, size < 1 ? 20 : size, ct)));

        group.MapPost("/{jobId}/rollback", [Authorize(Roles = "admin,operator")] async (
            string jobId, CekokDbContext db, CancellationToken ct) =>
        {
            // Rollback placeholder — mark as triggering rollback re-deploy
            var job = await db.DeployJobs.FindAsync([jobId], ct);
            if (job is null) return Results.NotFound();
            return Results.Ok(new { message = "Rollback triggered (restore from backup on target)" });
        });

        return group;
    }
}
