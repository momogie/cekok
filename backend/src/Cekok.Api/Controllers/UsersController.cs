using Cekok.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class UsersController
{
    public static RouteGroupBuilder MapUsersEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", [Authorize(Roles = "admin")] async (UserService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAllAsync(ct)));

        group.MapPost("/", [Authorize(Roles = "admin")] async (
            CreateUserRequest req, UserService svc, HttpContext ctx, CancellationToken ct) =>
        {
            var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var user = await svc.CreateAsync(req, userId, ct);
            return Results.Created($"/api/users/{user.Id}", user);
        });

        group.MapGet("/{id}", [Authorize(Roles = "admin")] async (
            string id, UserService svc, CancellationToken ct) =>
        {
            var user = await svc.GetByIdAsync(id, ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        group.MapPut("/{id}", [Authorize(Roles = "admin")] async (
            string id, UpdateUserRequest req, UserService svc, CancellationToken ct) =>
        {
            var user = await svc.UpdateAsync(id, req, ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        group.MapDelete("/{id}", [Authorize(Roles = "admin")] async (
            string id, UserService svc, CancellationToken ct) =>
        {
            var ok = await svc.DeleteAsync(id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapPut("/{id}/password", [Authorize(Roles = "admin")] async (
            string id, ResetPasswordDto dto, UserService svc, CancellationToken ct) =>
        {
            var ok = await svc.ResetPasswordAsync(id, dto.Password, ct);
            return ok ? Results.Ok() : Results.NotFound();
        });

        group.MapGet("/{id}/server-access", [Authorize(Roles = "admin")] async (
            string id, UserService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetServerAccessAsync(id, ct)));

        group.MapPut("/{id}/server-access", [Authorize(Roles = "admin")] async (
            string id, List<SetServerAccessRequest> body, UserService svc,
            HttpContext ctx, CancellationToken ct) =>
        {
            var grantedBy = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            await svc.SetServerAccessAsync(id, body, grantedBy, ct);
            return Results.Ok();
        });

        group.MapPost("/{id}/server-access/{serverId}", [Authorize(Roles = "admin")] async (
            string id, string serverId, ServerAccessDto dto,
            UserService svc, HttpContext ctx, CancellationToken ct) =>
        {
            var grantedBy = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var access = await svc.GrantServerAccessAsync(id, serverId, dto.CanDeploy, dto.CanManage, grantedBy, ct);
            return Results.Ok(access);
        });

        group.MapDelete("/{id}/server-access/{serverId}", [Authorize(Roles = "admin")] async (
            string id, string serverId, UserService svc, CancellationToken ct) =>
        {
            var ok = await svc.RevokeServerAccessAsync(id, serverId, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        return group;
    }

    public record ResetPasswordDto(string Password);
    public record ServerAccessDto(bool CanDeploy, bool CanManage);
}
