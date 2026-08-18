namespace Messenger.Web.Models;

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

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
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
}

public class ChangePasswordRequest
{
    public string Password { get; set; } = string.Empty;
}

public class ChatDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<UserDto> Participants { get; set; } = new();
    public UserDto? OtherUser { get; set; }
    public MessageDto? LastMessage { get; set; }
}

public class StartChatRequest
{
    public Guid TargetUserId { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class SendMessageRequest
{
    public string Text { get; set; } = string.Empty;
}

public class UpdateMessageRequest
{
    public string Text { get; set; } = string.Empty;
}

public class ApiError
{
    public string? Message { get; set; }
}
