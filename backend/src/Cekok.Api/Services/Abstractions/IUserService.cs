using Cekok.Api.Dtos;
using Cekok.Api.Models.Entities;

namespace Cekok.Api.Services.Abstractions;

public interface IUserService
{
    Task<List<User>> GetAllAsync(CancellationToken ct);
    Task<User?> GetByIdAsync(string id, CancellationToken ct);
    Task<User> CreateAsync(CreateUserRequest req, string createdBy, CancellationToken ct);
    Task<User?> UpdateAsync(string id, UpdateUserRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(string id, CancellationToken ct);
    Task<bool> ResetPasswordAsync(string id, string newPassword, CancellationToken ct);
    Task<List<UserServerAccess>> GetServerAccessAsync(string userId, CancellationToken ct);
    Task SetServerAccessAsync(string userId, List<SetServerAccessRequest> accesses, string grantedBy, CancellationToken ct);
    Task<UserServerAccess> GrantServerAccessAsync(string userId, string serverId, bool canDeploy, bool canManage, string grantedBy, CancellationToken ct);
    Task<bool> RevokeServerAccessAsync(string userId, string serverId, CancellationToken ct);
}
