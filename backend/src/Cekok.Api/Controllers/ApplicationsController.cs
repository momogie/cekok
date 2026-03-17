using Cekok.Api.Data;
using Cekok.Api.Models;
using Cekok.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class ApplicationsController
{
    public static RouteGroupBuilder MapApplicationsEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/applications — list apps (admin: all, operator/viewer: accessible only)
        group.MapGet("/", async (CekokDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (role == "admin")
                return Results.Ok(await db.Applications.ToListAsync(ct));

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

        // POST /api/applications — create app
        group.MapPost("/", [Authorize(Roles = "admin,operator")] async (
            CreateAppDto dto, CekokDbContext db, EncryptionService enc, CancellationToken ct) =>
        {
            var app = new Application
            {
                Name           = dto.Name,
                Type           = dto.Type,
                RepoUrl        = dto.RepoUrl,
                Branch         = dto.Branch ?? "main",
                BuildCmd       = dto.BuildCmd,
                OutputDir      = dto.OutputDir,
                Trigger        = dto.Trigger ?? "manual",
                TokenEnc       = string.IsNullOrWhiteSpace(dto.Token) ? null : enc.Encrypt(dto.Token),
                EnvVars        = dto.EnvVars != null ? JsonSerializer.Serialize(dto.EnvVars) : null,
                DeployDir      = dto.DeployDir,
                ServiceName    = dto.ServiceName,
                Port           = dto.Port,
                ScheduleCron   = dto.ScheduleCron,
                ScheduleEnabled = dto.ScheduleEnabled,
            };
            db.Applications.Add(app);

            // Legacy: also create a DeployTarget row if serverId provided
            if (dto.ServerId != null && dto.DeployDir != null)
            {
                db.DeployTargets.Add(new DeployTarget
                {
                    AppId       = app.Id,
                    ServerId    = dto.ServerId,
                    DeployDir   = dto.DeployDir,
                    ServiceName = dto.ServiceName,
                    Port        = dto.Port,
                });
            }

            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/applications/{app.Id}", app);
        });

        // GET /api/applications/{id}
        group.MapGet("/{id}", async (string id, CekokDbContext db, CancellationToken ct) =>
        {
            var app = await db.Applications.FindAsync([id], ct);
            return app is null ? Results.NotFound() : Results.Ok(app);
        });

        // PUT /api/applications/{id}
        group.MapPut("/{id}", [Authorize(Roles = "admin,operator")] async (
            string id, UpdateAppDto dto, CekokDbContext db, EncryptionService enc, CancellationToken ct) =>
        {
            var app = await db.Applications.FindAsync([id], ct);
            if (app is null) return Results.NotFound();

            if (dto.Name        != null) app.Name        = dto.Name;
            if (dto.RepoUrl     != null) app.RepoUrl     = dto.RepoUrl;
            if (dto.Branch      != null) app.Branch      = dto.Branch;
            if (dto.BuildCmd    != null) app.BuildCmd    = dto.BuildCmd;
            if (dto.OutputDir   != null) app.OutputDir   = dto.OutputDir;
            if (dto.Trigger     != null) app.Trigger     = dto.Trigger;
            if (dto.DeployDir   != null) app.DeployDir   = dto.DeployDir;
            if (dto.ServiceName != null) app.ServiceName = dto.ServiceName;
            if (dto.Port.HasValue)       app.Port        = dto.Port.Value;
            if (dto.ScheduleCron    != null)    app.ScheduleCron    = dto.ScheduleCron;
            if (dto.ScheduleEnabled.HasValue)   app.ScheduleEnabled = dto.ScheduleEnabled.Value;
            if (dto.EnvVars != null)
                app.EnvVars = JsonSerializer.Serialize(dto.EnvVars);
            if (!string.IsNullOrWhiteSpace(dto.Token))
                app.TokenEnc = enc.Encrypt(dto.Token!);

            await db.SaveChangesAsync(ct);
            return Results.Ok(app);
        });

        // DELETE /api/applications/{id}
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

    // ────── DTOs ──────

    /// <summary>Flat DTO matching AppForm.vue fields.</summary>
    public record CreateAppDto(
        string Name,
        string Type,
        string RepoUrl,
        string? Branch,
        string? BuildCmd,
        string? OutputDir,
        /// <summary>manual | webhook | schedule | both</summary>
        string? Trigger,
        /// <summary>GitHub Personal Access Token (plain — stored encrypted)</summary>
        string? Token,
        /// <summary>Environment variables [{key, val}]</summary>
        List<EnvVarDto>? EnvVars,
        /// <summary>Deploy target fields (single-server)</summary>
        string? DeployDir,
        string? ServiceName,
        int? Port,
        /// <summary>Optional: pre-existing server to link as DeployTarget</summary>
        string? ServerId,
        string? ScheduleCron,
        bool ScheduleEnabled
    );

    public record UpdateAppDto(
        string? Name,
        string? RepoUrl,
        string? Branch,
        string? BuildCmd,
        string? OutputDir,
        string? Trigger,
        string? Token,
        List<EnvVarDto>? EnvVars,
        string? DeployDir,
        string? ServiceName,
        int? Port,
        string? ScheduleCron,
        bool? ScheduleEnabled
    );

    public record EnvVarDto(string Key, string Val);

    // Kept for backwards compat with other parts of the codebase
    public record DeployTargetDto(string ServerId, string DeployDir,
        string? ServiceName, int? Port, string? HealthCheckUrl);
}
