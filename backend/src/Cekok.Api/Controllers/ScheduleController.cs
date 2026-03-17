using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class ScheduleController
{
    public static RouteGroupBuilder MapScheduleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (CekokDbContext db, CancellationToken ct) =>
        {
            var apps = await db.Applications
                .Select(a => new { a.Id, a.Name, a.ScheduleEnabled, a.ScheduleCron })
                .ToListAsync(ct);
            return Results.Ok(apps);
        });

        group.MapPut("/{appId}", [Authorize(Roles = "admin,operator")] async (
            string appId, UpdateScheduleDto dto, CekokDbContext db, CancellationToken ct) =>
        {
            var app = await db.Applications.FindAsync([appId], ct);
            if (app is null) return Results.NotFound();
            app.ScheduleCron = dto.Cron;
            app.ScheduleEnabled = dto.Enabled;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { app.Id, app.ScheduleEnabled, app.ScheduleCron });
        });

        group.MapGet("/audit", [Authorize(Roles = "admin")] async (
            int page, CekokDbContext db, CancellationToken ct) =>
        {
            var logs = await db.AuditLogs
                .OrderByDescending(l => l.Id)
                .Skip(page * 50).Take(50)
                .ToListAsync(ct);
            return Results.Ok(logs);
        });

        return group;
    }

    public record UpdateScheduleDto(string? Cron, bool Enabled);
}
