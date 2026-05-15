using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class UserServerAccess
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string UserId { get; set; } = "";
    [Required] public string ServerId { get; set; } = "";
    public bool CanDeploy { get; set; } = true;
    public bool CanManage { get; set; } = false;
    [Required] public string GrantedBy { get; set; } = "";
    public string GrantedAt { get; set; } = DateTime.UtcNow.ToString("O");
}
