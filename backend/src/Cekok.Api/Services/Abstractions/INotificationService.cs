using Cekok.Api.Models.Entities;

namespace Cekok.Api.Services.Abstractions;

public interface INotificationService
{
    Task SendDeploymentNotificationAsync(Application app, DeployJob job, Func<string, string, Task>? log = null);
    Task SendTestNotificationAsync(Application app, string type);
}
