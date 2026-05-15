using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cekok.Api.Models.Entities;

public class DeployLog
{
    [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required] public string JobId { get; set; } = "";
    public string? ServerId { get; set; }
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
    [Required] public string Level { get; set; } = "info";
    [Required] public string Message { get; set; } = "";
}
