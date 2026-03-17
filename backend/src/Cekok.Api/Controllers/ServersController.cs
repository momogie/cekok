using Cekok.Api.Data;
using Cekok.Api.Models;
using Cekok.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class ServersController
{
    public static RouteGroupBuilder MapServersEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (CekokDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role == "admin")
                return Results.Ok(await db.Servers.ToListAsync(ct));

            var accessibleIds = await db.UserServerAccesses
                .Where(a => a.UserId == userId)
                .Select(a => a.ServerId)
                .ToListAsync(ct);
            var servers = await db.Servers.Where(s => accessibleIds.Contains(s.Id)).ToListAsync(ct);
            return Results.Ok(servers);
        });

        group.MapPost("/", [Authorize(Roles = "admin")] async (
            CreateServerDto dto, CekokDbContext db, EncryptionService enc, CancellationToken ct) =>
        {
            var server = new Server
            {
                Name = dto.Name, Ip = dto.Ip, SshPort = dto.SshPort,
                SshUser = dto.SshUser,
                SshPasswordEnc = enc.Encrypt(dto.SshPassword),
                Role = dto.Role, Tags = dto.Tags,
            };
            db.Servers.Add(server);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/servers/{server.Id}", server);
        });

        group.MapDelete("/{id}", [Authorize(Roles = "admin")] async (
            string id, CekokDbContext db, CancellationToken ct) =>
        {
            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            db.Servers.Remove(srv);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapPost("/{id}/test-connection", async (
            string id, CekokDbContext db, SshService ssh, EncryptionService enc, CancellationToken ct) =>
        {
            var srv = await db.Servers.FindAsync([id], ct);
            if (srv is null) return Results.NotFound();
            var pw = enc.Decrypt(srv.SshPasswordEnc);
            var ok = await ssh.TestConnectionAsync(srv.Ip, srv.SshPort, srv.SshUser, pw, ct);
            return Results.Ok(new { connected = ok });
        });

        return group;
    }

    public record CreateServerDto(string Name, string Ip, int SshPort, string SshUser,
        string SshPassword, string Role, string? Tags);
}
