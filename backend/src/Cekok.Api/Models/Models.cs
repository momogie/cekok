using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cekok.Api.Models;

public enum UserRole { admin, @operator, viewer }

public class User
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Username { get; set; } = "";
    [Required] public string DisplayName { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.viewer;
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string? LastLoginAt { get; set; }
}

public class UserServerAccess
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string UserId { get; set; } = "";
    [Required] public string ServerId { get; set; } = "";
    public bool CanDeploy { get; set; } = true;
    public bool CanManage { get; set; } = false;
    [Required] public string GrantedBy { get; set; } = "";
    public string GrantedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

public class RefreshToken
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string UserId { get; set; } = "";
    [Required] public string TokenHash { get; set; } = "";
    [Required] public string ExpiresAt { get; set; } = "";
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string? RevokedAt { get; set; }
}

public class AuditLog
{
    [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = "";
    [Required] public string Action { get; set; } = "";
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

public class Server
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = "";
    [Required] public string Ip { get; set; } = "";
    public int SshPort { get; set; } = 22;
    [Required] public string SshUser { get; set; } = "";
    [Required] public string SshPasswordEnc { get; set; } = "";
    [Required] public string Role { get; set; } = "app-server"; // master | app-server | proxy | db-server
    public string? Hostname { get; set; }
    public string? Tags { get; set; } // JSON array
    public bool NginxInstalled { get; set; } = false;
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

public class Application
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = "";
    [Required] public string Type { get; set; } = "dotnet"; // dotnet | nuxt | vue | next | angular | static
    [Required] public string RepoUrl { get; set; } = "";
    public string Branch { get; set; } = "main";
    public string? BuildCmd { get; set; }
    public string? OutputDir { get; set; }
    /// <summary>manual | webhook | schedule | both</summary>
    public string Trigger { get; set; } = "manual";
    /// <summary>GitHub Personal Access Token (encrypted)</summary>
    public string? TokenEnc { get; set; }
    /// <summary>Environment variables as JSON array [{key,val}]</summary>
    public string? EnvVars { get; set; }
    // Deploy target (single-server form)
    public string? DeployDir { get; set; }
    public string? ServiceName { get; set; }
    public int? Port { get; set; }
    public string? ScheduleCron { get; set; }
    public bool ScheduleEnabled { get; set; } = false;
    public string? EntryFile { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    [NotMapped]
    public List<DeployTarget> DeployTargets { get; set; } = new();
}

public class DeployTarget
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string AppId { get; set; } = "";
    [Required] public string ServerId { get; set; } = "";
    [Required] public string DeployDir { get; set; } = "";
    public string? ServiceName { get; set; }
    public int? Port { get; set; }
    public string? HealthCheckUrl { get; set; }
    public string Status { get; set; } = "idle"; // idle | success | failed
}

public class DeployJob
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string AppId { get; set; } = "";
    [Required] public string TriggeredBy { get; set; } = "manual"; // manual | schedule | webhook
    public string? TriggeredByUser { get; set; }
    [Required] public string Status { get; set; } = "queued"; // queued | running | success | failed
    public string? CommitHash { get; set; }
    public string? CommitMsg { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string? StartedAt { get; set; }
    public string? FinishedAt { get; set; }
}

public class DeployLog
{
    [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required] public string JobId { get; set; } = "";
    public string? ServerId { get; set; } // NULL = master/build phase
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
    [Required] public string Level { get; set; } = "info"; // info | cmd | success | warn | error
    [Required] public string Message { get; set; } = "";
}

public class NginxConfig
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string ServerId { get; set; } = "";
    [Required] public string SiteName { get; set; } = "";
    [Required] public string TemplateType { get; set; } = "reverse_proxy"; // reverse_proxy | static_site | upstream
    [Required] public string ConfigContent { get; set; } = "";
    public string? LastDeployedAt { get; set; }
}

/// <summary>
/// A config file (e.g. appsettings.json, google/fcm.json) that should be written
/// to the deploy directory at deploy time. Content is stored encrypted.
/// </summary>
public class AppSettingFile
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>FK → Application.Id</summary>
    [Required] public string AppId { get; set; } = "";
    /// <summary>
    /// Relative path from the app root, e.g. "appsettings.json" or "google/fcm.json".
    /// </summary>
    [Required] public string FilePath { get; set; } = "";
    /// <summary>File content encrypted with EncryptionService (AES-256).</summary>
    [Required] public string ContentEnc { get; set; } = "";
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}
