using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class TelegramSubscriber
{
    [Key] public string ChatId { get; set; } = "";
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public bool IsActive { get; set; } = true;
}
