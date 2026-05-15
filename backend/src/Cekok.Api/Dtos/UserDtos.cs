namespace Cekok.Api.Dtos;

public record CreateUserRequest(string Username, string DisplayName, string Password, string Role);
public record UpdateUserRequest(string? DisplayName, string? Role, bool? IsActive);
public record SetServerAccessRequest(string ServerId, bool CanDeploy, bool CanManage);
public record ResetPasswordDto(string Password);
public record ServerAccessDto(bool CanDeploy, bool CanManage);
