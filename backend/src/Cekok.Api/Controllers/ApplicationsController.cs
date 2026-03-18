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

            List<Application> apps;
            if (role == "admin")
            {
                apps = await db.Applications.ToListAsync(ct);
            }
            else
            {
                var accessibleServerIds = await db.UserServerAccesses
                    .Where(a => a.UserId == userId)
                    .Select(a => a.ServerId)
                    .ToListAsync(ct);
                var accessibleAppIds = await db.DeployTargets
                    .Where(t => accessibleServerIds.Contains(t.ServerId))
                    .Select(t => t.AppId)
                    .Distinct()
                    .ToListAsync(ct);
                apps = await db.Applications.Where(a => accessibleAppIds.Contains(a.Id)).ToListAsync(ct);
            }

            var appIds = apps.Select(a => a.Id).ToList();
            var allTargets = await db.DeployTargets.Where(t => appIds.Contains(t.AppId)).ToListAsync(ct);
            foreach (var a in apps)
            {
                a.DeployTargets = allTargets.Where(t => t.AppId == a.Id).ToList();
            }
            
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
                EntryFile      = dto.EntryFile,
                NotifyEmail    = dto.NotifyEmail,
                NotifyEmailAddress = dto.NotifyEmailAddress,
                NotifyTelegram = dto.NotifyTelegram,
                NotifyTelegramChatId = dto.NotifyTelegramChatId,
            };
            db.Applications.Add(app);

        // Settings Files
        if (dto.SettingFiles != null && dto.SettingFiles.Count > 0)
        {
            foreach (var s in dto.SettingFiles)
            {
                db.AppSettingFiles.Add(new AppSettingFile
                {
                    AppId = app.Id,
                    FilePath = s.FilePath,
                    ContentEnc = enc.Encrypt(s.Content)
                });
            }
        }

        // Multi-server deploy targets from new form
        if (dto.DeployTargets != null && dto.DeployTargets.Count > 0)
        {
            foreach (var t in dto.DeployTargets)
            {
                db.DeployTargets.Add(new DeployTarget
                {
                    AppId          = app.Id,
                    ServerId       = t.ServerId,
                    DeployDir      = t.DeployDir,
                    ServiceName    = t.ServiceName,
                    Port           = t.Port,
                    HealthCheckUrl = t.HealthCheckUrl,
                });
            }
        }
        // Legacy: also create a DeployTarget row if single serverId provided
        else if (dto.ServerId != null && dto.DeployDir != null)
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
            if (app is null) return Results.NotFound();
            
            app.DeployTargets = await db.DeployTargets.Where(t => t.AppId == id).ToListAsync(ct);
            return Results.Ok(app);
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
            if (dto.ScheduleCron    != null)    app.ScheduleCron    = dto.ScheduleCron;
            if (dto.ScheduleEnabled.HasValue)   app.ScheduleEnabled = dto.ScheduleEnabled.Value;
            if (dto.EntryFile       != null)    app.EntryFile       = dto.EntryFile;
            if (dto.NotifyEmail.HasValue)       app.NotifyEmail     = dto.NotifyEmail.Value;
            if (dto.NotifyEmailAddress != null) app.NotifyEmailAddress = dto.NotifyEmailAddress;
            if (dto.NotifyTelegram.HasValue)    app.NotifyTelegram  = dto.NotifyTelegram.Value;
            if (dto.NotifyTelegramChatId != null) app.NotifyTelegramChatId = dto.NotifyTelegramChatId;
            if (dto.EnvVars != null)
                app.EnvVars = JsonSerializer.Serialize(dto.EnvVars);
            if (!string.IsNullOrWhiteSpace(dto.Token))
                app.TokenEnc = enc.Encrypt(dto.Token!);

            // Sync deploy targets if provided
            if (dto.DeployTargets != null)
            {
                var existing = await db.DeployTargets.Where(t => t.AppId == id).ToListAsync(ct);
                db.DeployTargets.RemoveRange(existing);
                foreach (var t in dto.DeployTargets)
                {
                    db.DeployTargets.Add(new DeployTarget
                    {
                        AppId          = id,
                        ServerId       = t.ServerId,
                        DeployDir      = t.DeployDir,
                        ServiceName    = t.ServiceName,
                        Port           = t.Port,
                        HealthCheckUrl = t.HealthCheckUrl,
                    });
                }
            }

            // Sync setting files
            if (dto.SettingFiles != null)
            {
                var existingSettings = await db.AppSettingFiles.Where(s => s.AppId == id).ToListAsync(ct);
                db.AppSettingFiles.RemoveRange(existingSettings);
                foreach (var s in dto.SettingFiles)
                {
                    db.AppSettingFiles.Add(new AppSettingFile
                    {
                        AppId = id,
                        FilePath = s.FilePath,
                        ContentEnc = enc.Encrypt(s.Content)
                    });
                }
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(app);
        });

        // GET /api/applications/{id}/settings
        group.MapGet("/{id}/settings", [Authorize(Roles = "admin,operator")] async (string id, CekokDbContext db, EncryptionService enc, CancellationToken ct) =>
        {
            var settings = await db.AppSettingFiles.Where(s => s.AppId == id).ToListAsync(ct);
            var result = settings.Select(s => new {
                s.FilePath,
                Content = enc.Decrypt(s.ContentEnc)
            });
            return Results.Ok(result);
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

        // POST /api/applications/{id}/notify-test
        group.MapPost("/{id}/notify-test", [Authorize(Roles = "admin,operator")] async (
            string id, string type, CekokDbContext db, NotificationService notify, CancellationToken ct) =>
        {
            var app = await db.Applications.FindAsync([id], ct);
            if (app is null) return Results.NotFound();

            await notify.SendTestNotificationAsync(app, type);
            return Results.Ok(new { message = $"Test {type} notification sent" });
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
        List<SettingFileDto>? SettingFiles,
        /// <summary>Multi-server deploy targets (new form)</summary>
        List<DeployTargetDto>? DeployTargets,
        /// <summary>Legacy single-server fields (backward compat)</summary>
        string? DeployDir,
        string? ServiceName,
        int? Port,
        string? ServerId,
        string? ScheduleCron,
        bool ScheduleEnabled,
        string? EntryFile,
        bool NotifyEmail,
        string? NotifyEmailAddress,
        bool NotifyTelegram,
        string? NotifyTelegramChatId
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
        List<SettingFileDto>? SettingFiles,
        /// <summary>Multi-server deploy targets — replaces all existing targets when provided</summary>
        List<DeployTargetDto>? DeployTargets,
        string? ScheduleCron,
        bool? ScheduleEnabled,
        string? EntryFile,
        bool? NotifyEmail,
        string? NotifyEmailAddress,
        bool? NotifyTelegram,
        string? NotifyTelegramChatId
    );

    public record EnvVarDto(string Key, string Val);
    public record SettingFileDto(string FilePath, string Content);

    // Kept for backwards compat with other parts of the codebase
    public record DeployTargetDto(string ServerId, string DeployDir,
        string? ServiceName, int? Port, string? HealthCheckUrl);
}
