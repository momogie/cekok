namespace Cekok.Api.Dtos;

public record CreateServerDto(string Name, string Ip, int SshPort, string SshUser,
    string SshPassword, string? SshSuPassword, string Role, string? Tags);

public record UpdateServerDto(string Name, string Ip, int SshPort, string SshUser,
    string? SshPassword, string? SshSuPassword, string Role, string? Tags);

public record ExecuteCmdDto(string Command);
