namespace Cekok.Api.Dtos;

public record UpdateSettingRequest(string Key, string Value, string? Group, bool IsSecure);
