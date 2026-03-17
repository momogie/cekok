using Cekok.Api.Data;
using Cekok.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cekok.Api.Services;

public record CreateUserRequest(string Username, string DisplayName, string Password, string Role);
public record UpdateUserRequest(string? DisplayName, string? Role, bool? IsActive);
public record SetServerAccessRequest(string ServerId, bool CanDeploy, bool CanManage);

public class UserService(CekokDbContext db)
{
    public async Task<List<User>> GetAllAsync(CancellationToken ct) =>
        await db.Users.OrderBy(u => u.Username).ToListAsync(ct);

    public async Task<User?> GetByIdAsync(string id, CancellationToken ct) =>
        await db.Users.FindAsync([id], ct);

    public async Task<User> CreateAsync(CreateUserRequest req, string createdBy, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(req.Role, out var role))
            throw new ArgumentException($"Invalid role: {req.Role}");

        var user = new User
        {
            Username = req.Username.Trim(),
            DisplayName = req.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = role,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User?> UpdateAsync(string id, UpdateUserRequest req, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return null;
        if (req.DisplayName is not null) user.DisplayName = req.DisplayName;
        if (req.IsActive is not null) user.IsActive = req.IsActive.Value;
        if (req.Role is not null && Enum.TryParse<UserRole>(req.Role, out var role))
            user.Role = role;
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string id, string newPassword, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<UserServerAccess>> GetServerAccessAsync(string userId, CancellationToken ct) =>
        await db.UserServerAccesses.Where(u => u.UserId == userId).ToListAsync(ct);

    public async Task SetServerAccessAsync(string userId, List<SetServerAccessRequest> accesses,
        string grantedBy, CancellationToken ct)
    {
        var existing = await db.UserServerAccesses.Where(u => u.UserId == userId).ToListAsync(ct);
        db.UserServerAccesses.RemoveRange(existing);
        db.UserServerAccesses.AddRange(accesses.Select(a => new UserServerAccess
        {
            UserId = userId,
            ServerId = a.ServerId,
            CanDeploy = a.CanDeploy,
            CanManage = a.CanManage,
            GrantedBy = grantedBy,
        }));
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserServerAccess> GrantServerAccessAsync(string userId, string serverId,
        bool canDeploy, bool canManage, string grantedBy, CancellationToken ct)
    {
        var existing = await db.UserServerAccesses
            .FirstOrDefaultAsync(u => u.UserId == userId && u.ServerId == serverId, ct);
        if (existing is not null)
        {
            existing.CanDeploy = canDeploy;
            existing.CanManage = canManage;
            await db.SaveChangesAsync(ct);
            return existing;
        }
        var access = new UserServerAccess
        {
            UserId = userId, ServerId = serverId,
            CanDeploy = canDeploy, CanManage = canManage, GrantedBy = grantedBy
        };
        db.UserServerAccesses.Add(access);
        await db.SaveChangesAsync(ct);
        return access;
    }

    public async Task<bool> RevokeServerAccessAsync(string userId, string serverId, CancellationToken ct)
    {
        var access = await db.UserServerAccesses
            .FirstOrDefaultAsync(u => u.UserId == userId && u.ServerId == serverId, ct);
        if (access is null) return false;
        db.UserServerAccesses.Remove(access);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
