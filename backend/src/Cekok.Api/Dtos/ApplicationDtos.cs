namespace Cekok.Api.Dtos;

public record CreateAppDto(
    string Name, string Type, string RepoUrl, string? Branch, string? BuildCmd,
    string? OutputDir, string? Trigger, string? Token, List<EnvVarDto>? EnvVars,
    List<SettingFileDto>? SettingFiles, List<DeployTargetDto>? DeployTargets,
    string? DeployDir, string? ServiceName, int? Port, string? ServerId,
    string? ScheduleCron, bool ScheduleEnabled, string? EntryFile,
    bool NotifyEmail, string? NotifyEmailAddress,
    bool NotifyTelegram, string? NotifyTelegramChatId);

public record UpdateAppDto(
    string? Name, string? RepoUrl, string? Branch, string? BuildCmd,
    string? OutputDir, string? Trigger, string? Token, List<EnvVarDto>? EnvVars,
    List<SettingFileDto>? SettingFiles, List<DeployTargetDto>? DeployTargets,
    string? ScheduleCron, bool? ScheduleEnabled, string? EntryFile,
    bool? NotifyEmail, string? NotifyEmailAddress,
    bool? NotifyTelegram, string? NotifyTelegramChatId);

public record EnvVarDto(string Key, string Val);
public record SettingFileDto(string FilePath, string Content);
public record DeployTargetDto(string ServerId, string DeployDir,
    string? ServiceName, int? Port, string? HealthCheckUrl);
