using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class ApplicationsController
{
    public static RouteGroupBuilder MapApplicationsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (CekokDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (role == "admin")
                return Results.Ok(await db.Applications.ToListAsync(ct));

            // Operator/viewer: only apps deployed on accessible servers
            var accessibleServerIds = await db.UserServerAccesses
                .Where(a => a.UserId == userId)
                .Select(a => a.ServerId)
                .ToListAsync(ct);
            var accessibleAppIds = await db.DeployTargets
                .Where(t => accessibleServerIds.Contains(t.ServerId))
                .Select(t => t.AppId)
                .Distinct()
                .ToListAsync(ct);
            var apps = await db.Applications.Where(a => accessibleAppIds.Contains(a.Id)).ToListAsync(ct);
            return Results.Ok(apps);
        });

        group.MapPost("/", [Authorize(Roles = "admin,operator")] async (
            CreateAppDto dto, CekokDbContext db, CancellationToken ct) =>
        {
            var app = new Application
            {
                Name = dto.Name, Type = dto.Type, RepoUrl = dto.RepoUrl,
                Branch = dto.Branch ?? "main", BuildCmd = dto.BuildCmd, OutputDir = dto.OutputDir,
                ScheduleCron = dto.ScheduleCron, ScheduleEnabled = dto.ScheduleEnabled,
            };
            db.Applications.Add(app);

            if (dto.Targets != null)
                db.DeployTargets.AddRange(dto.Targets.Select(t => new DeployTarget
                {
                    AppId = app.Id, ServerId = t.ServerId, DeployDir = t.DeployDir,
                    ServiceName = t.ServiceName, Port = t.Port, HealthCheckUrl = t.HealthCheckUrl,
                }));

            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/applications/{app.Id}", app);
        });

        group.MapGet("/{id}", async (string id, CekokDbContext db, CancellationToken ct) =>
        {
            var app = await db.Applications.FindAsync([id], ct);
            return app is null ? Results.NotFound() : Results.Ok(app);
        });

        group.MapPut("/{id}", [Authorize(Roles = "admin,operator")] async (
            string id, UpdateAppDto dto, CekokDbContext db, CancellationToken ct) =>
        {
            var app = await db.Applications.FindAsync([id], ct);
            if (app is null) return Results.NotFound();
            if (dto.Name != null) app.Name = dto.Name;
            if (dto.Branch != null) app.Branch = dto.Branch;
            if (dto.BuildCmd != null) app.BuildCmd = dto.BuildCmd;
            if (dto.OutputDir != null) app.OutputDir = dto.OutputDir;
            if (dto.ScheduleCron != null) app.ScheduleCron = dto.ScheduleCron;
            if (dto.ScheduleEnabled.HasValue) app.ScheduleEnabled = dto.ScheduleEnabled.Value;
            await db.SaveChangesAsync(ct);
            return Results.Ok(app);
        });

        group.MapDelete("/{id}", [Authorize(Roles = "admin")] async (
            string id, CekokDbContext db, CancellationToken ct) =>
        {
            var app = await db.Applications.FindAsync([id], ct);
            if (app is null) return Results.NotFound();
            db.Applications.Remove(app);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return group;
    }

    public record DeployTargetDto(string ServerId, string DeployDir,
        string? ServiceName, int? Port, string? HealthCheckUrl);

    public record CreateAppDto(string Name, string Type, string RepoUrl,
        string? Branch, string? BuildCmd, string? OutputDir,
        string? ScheduleCron, bool ScheduleEnabled,
        List<DeployTargetDto>? Targets);

    public record UpdateAppDto(string? Name, string? Branch, string? BuildCmd,
        string? OutputDir, string? ScheduleCron, bool? ScheduleEnabled);
}
