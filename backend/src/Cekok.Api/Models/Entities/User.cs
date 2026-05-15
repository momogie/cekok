using System.ComponentModel.DataAnnotations;
using Cekok.Api.Models.Enums;

namespace Cekok.Api.Models.Entities;

public class User
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Username { get; set; } = "";
    [Required] public string DisplayName { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.viewer;
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string? LastLoginAt { get; set; }
}
