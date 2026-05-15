using System.ComponentModel.DataAnnotations;

namespace Cekok.Api.Models.Entities;

public class Server
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = "";
    [Required] public string Ip { get; set; } = "";
    public int SshPort { get; set; } = 22;
    [Required] public string SshUser { get; set; } = "";
    [Required] public string SshPasswordEnc { get; set; } = "";
    public string? SshSuPasswordEnc { get; set; }
    [Required] public string Role { get; set; } = "app-server";
    public string? Hostname { get; set; }
    public string? Tags { get; set; }
    public bool NginxInstalled { get; set; } = false;
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}
