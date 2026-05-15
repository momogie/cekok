using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class SystemSetting
{
    [Key] public string Key { get; set; } = "";
    [Required] public string Value { get; set; } = "";
    public string Group { get; set; } = "general";
    public bool IsSecure { get; set; } = false;
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}
