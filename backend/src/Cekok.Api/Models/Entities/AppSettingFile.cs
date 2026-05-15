using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class AppSettingFile
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string AppId { get; set; } = "";
    [Required] public string FilePath { get; set; } = "";
    [Required] public string ContentEnc { get; set; } = "";
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}
