using Messenger.Api.Data;
using Messenger.Api.DTOs;
using Messenger.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Api.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserDto>> GetUsersAsync(bool isAdmin)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (!isAdmin)
        {
            query = query.Where(u => u.IsActive);
        }

        var users = await query.OrderBy(u => u.UserName).ToListAsync();
        return users.Select(Map).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        ValidateRole(request.Role);
        var userName = NormalizeUserName(request.UserName);
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AppException("Пароль обязателен.");
        }

        await EnsureUserNameIsFreeAsync(userName);

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            DisplayName = NormalizeDisplayName(request.DisplayName, userName),
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Map(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        ValidateRole(request.Role);
        var user = await GetRequiredAsync(id);
        var userName = NormalizeUserName(request.UserName);
        await EnsureUserNameIsFreeAsync(userName, id);

        user.UserName = userName;
        user.DisplayName = NormalizeDisplayName(request.DisplayName, userName);
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return Map(user);
    }

    public async Task DeactivateAsync(Guid id, Guid currentUserId)
    {
        if (id == currentUserId)
        {
            throw new AppException("Нельзя деактивировать собственную учётную запись.");
        }

        var user = await GetRequiredAsync(id);
        user.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid id, ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AppException("Пароль обязателен.");
        }

        var user = await GetRequiredAsync(id);
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        await _db.SaveChangesAsync();
    }

    public static UserDto Map(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    private async Task<User> GetRequiredAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            throw new AppException("Пользователь не найден.", StatusCodes.Status404NotFound);
        }

        return user;
    }

    private async Task EnsureUserNameIsFreeAsync(string userName, Guid? exceptUserId = null)
    {
        var exists = await _db.Users.AnyAsync(u =>
            u.UserName == userName && (!exceptUserId.HasValue || u.Id != exceptUserId.Value));
        if (exists)
        {
            throw new AppException("Пользователь с таким именем уже существует.", StatusCodes.Status409Conflict);
        }
    }

    private static void ValidateRole(string role)
    {
        if (!UserRoles.IsValid(role))
        {
            throw new AppException("Роль должна быть User или Admin.");
        }
    }

    private static string NormalizeUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new AppException("Имя пользователя обязательно.");
        }

        return userName.Trim();
    }

    private static string NormalizeDisplayName(string? displayName, string userName) =>
        string.IsNullOrWhiteSpace(displayName) ? userName : displayName.Trim();
}
