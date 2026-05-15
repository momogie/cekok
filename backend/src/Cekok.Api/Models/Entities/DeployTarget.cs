using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class DeployTarget
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string AppId { get; set; } = "";
    [Required] public string ServerId { get; set; } = "";
    [Required] public string DeployDir { get; set; } = "";
    public string? ServiceName { get; set; }
    public int? Port { get; set; }
    public string? HealthCheckUrl { get; set; }
    public string Status { get; set; } = "idle";
}
