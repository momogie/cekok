using Cekok.Api.Controllers;
using Cekok.Api.Data;
using Cekok.Api.Models;
using Cekok.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Serilog;
using System.Text;
using Yarp.ReverseProxy.Configuration;


var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// ── EF Core / SQLite ──
builder.Services.AddDbContext<CekokDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// ── JWT Auth ──
var jwtCfg = builder.Configuration.GetSection("Jwt");
var jwtSecret = Environment.GetEnvironmentVariable("CEKOK_JWT_SECRET")
    ?? jwtCfg["Secret"]
    ?? throw new Exception("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtCfg["Issuer"],
            ValidAudience = jwtCfg["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Services ──
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DeployService>();
builder.Services.AddScoped<SshService>();
builder.Services.AddScoped<ScpService>();
builder.Services.AddScoped<BuildService>();
builder.Services.AddScoped<NginxService>();
builder.Services.AddScoped<SystemAppService>();
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<HealthCheckService>();

builder.Services.AddHttpClient();

// ── CORS ──
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4010", "https://localhost:54387")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// ── Swagger / OpenAPI ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── YARP Reverse Proxy (Development Only) ──
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
}


var app = builder.Build();

// ── Migrate DB & Seed ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CekokDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any(u => u.Username == "momogie"))
    {
        db.Users.Add(new User
        {
            Username = "momogie",
            DisplayName = "Momogie",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("momogie123"),
            Role = UserRole.admin,
            IsActive = true
        });
        db.SaveChanges();
    }
}

app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Route groups ──
var api = app.MapGroup("/api");

// Auth endpoints
api.MapGroup("/auth").MapAuthEndpoints();
// Users endpoints
api.MapGroup("/users").MapUsersEndpoints().RequireAuthorization();
// Servers endpoints
api.MapGroup("/servers").MapServersEndpoints().RequireAuthorization();
// Applications endpoints
api.MapGroup("/applications").MapApplicationsEndpoints().RequireAuthorization();
// Deploy endpoints
api.MapGroup("/deploy").MapDeployEndpoints().RequireAuthorization();
// Nginx endpoints
api.MapGroup("/nginx").MapNginxEndpoints().RequireAuthorization();
// System Apps endpoints
api.MapGroup("/system-apps").MapSystemAppsEndpoints().RequireAuthorization();
// System env endpoints
api.MapGroup("/system").MapSystemEndpoints().RequireAuthorization();
// Schedule endpoints
api.MapGroup("/schedule").MapScheduleEndpoints().RequireAuthorization();

// ── Map YARP (Development Only) ──
if (app.Environment.IsDevelopment())
{
    app.UseWebSockets();
    app.MapReverseProxy();
}

app.Run();
