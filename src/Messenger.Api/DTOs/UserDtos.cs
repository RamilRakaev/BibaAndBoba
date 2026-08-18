using System.ComponentModel.DataAnnotations;

namespace Messenger.Api.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    [Required]
    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    [Required]
    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;
}

public class ChangePasswordRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
