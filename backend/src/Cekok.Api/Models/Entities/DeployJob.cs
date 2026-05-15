using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class DeployJob
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string AppId { get; set; } = "";
    [Required] public string TriggeredBy { get; set; } = "manual";
    public string? TriggeredByUser { get; set; }
    [Required] public string Status { get; set; } = "queued";
    public string? CommitHash { get; set; }
    public string? CommitMsg { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string? StartedAt { get; set; }
    public string? FinishedAt { get; set; }
}
