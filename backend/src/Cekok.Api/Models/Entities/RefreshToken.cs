using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class RefreshToken
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string UserId { get; set; } = "";
    [Required] public string TokenHash { get; set; } = "";
    [Required] public string ExpiresAt { get; set; } = "";
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string? RevokedAt { get; set; }
}
