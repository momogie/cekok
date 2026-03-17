using Cekok.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cekok.Api.Controllers;

public static class AuthController
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (LoginRequest req, AuthService svc, CancellationToken ct) =>
        {
            var result = await svc.LoginAsync(req, ct);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        });

        group.MapPost("/refresh", async (RefreshRequest req, AuthService svc, CancellationToken ct) =>
        {
            var result = await svc.RefreshAsync(req, ct);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        });

        group.MapPost("/logout", async (RefreshRequest req, AuthService svc, CancellationToken ct) =>
        {
            await svc.LogoutAsync(req.RefreshToken, ct);
            return Results.Ok();
        }).RequireAuthorization();

        group.MapGet("/me", (HttpContext ctx) =>
        {
            var sub = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var name = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var display = ctx.User.FindFirst("displayName")?.Value;
            return Results.Ok(new { id = sub, username = name, role, displayName = display });
        }).RequireAuthorization();

        return group;
    }
}
