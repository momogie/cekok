using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cekok.Api.Models.Entities;

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
