using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class Application
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = "";
    [Required] public string Type { get; set; } = "dotnet";
    [Required] public string RepoUrl { get; set; } = "";
    public string Branch { get; set; } = "main";
    public string? BuildCmd { get; set; }
    public string? OutputDir { get; set; }
    public string Trigger { get; set; } = "manual";
    public string? TokenEnc { get; set; }
    public string? EnvVars { get; set; }
    public string? DeployDir { get; set; }
    public string? ServiceName { get; set; }
    public int? Port { get; set; }
    public string? ScheduleCron { get; set; }
    public bool ScheduleEnabled { get; set; } = false;
    public string? EntryFile { get; set; }
    public bool NotifyEmail { get; set; } = false;
    public string? NotifyEmailAddress { get; set; }
    public bool NotifyTelegram { get; set; } = false;
    public string? NotifyTelegramChatId { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    [NotMapped]
    public List<DeployTarget> DeployTargets { get; set; } = new();
}
