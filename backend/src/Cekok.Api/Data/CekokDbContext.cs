using Microsoft.EntityFrameworkCore;
using Cekok.Api.Models;

namespace Cekok.Api.Data;

public class CekokDbContext(DbContextOptions<CekokDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserServerAccess> UserServerAccesses => Set<UserServerAccess>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<DeployTarget> DeployTargets => Set<DeployTarget>();
    public DbSet<DeployJob> DeployJobs => Set<DeployJob>();
    public DbSet<DeployLog> DeployLogs => Set<DeployLog>();
    public DbSet<NginxConfig> NginxConfigs => Set<NginxConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<UserServerAccess>(e =>
        {
            e.HasIndex(u => new { u.UserId, u.ServerId }).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(u => u.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Server>().WithMany().HasForeignKey(u => u.ServerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(r => r.TokenHash).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeployTarget>(e =>
        {
            e.HasOne<Application>().WithMany().HasForeignKey(d => d.AppId);
            e.HasOne<Server>().WithMany().HasForeignKey(d => d.ServerId);
        });

        modelBuilder.Entity<DeployJob>(e =>
        {
            e.HasOne<Application>().WithMany().HasForeignKey(d => d.AppId);
        });

        modelBuilder.Entity<DeployLog>(e =>
        {
            e.HasOne<DeployJob>().WithMany().HasForeignKey(d => d.JobId);
        });
    }
}
