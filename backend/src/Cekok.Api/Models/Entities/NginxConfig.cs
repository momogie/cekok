using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class NginxConfig
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string ServerId { get; set; } = "";
    [Required] public string SiteName { get; set; } = "";
    [Required] public string TemplateType { get; set; } = "reverse_proxy";
    [Required] public string ConfigContent { get; set; } = "";
    public string? LastDeployedAt { get; set; }
}
