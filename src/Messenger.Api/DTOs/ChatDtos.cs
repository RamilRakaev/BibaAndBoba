using System.ComponentModel.DataAnnotations;

namespace Messenger.Api.DTOs;

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
    [Required]
    public Guid TargetUserId { get; set; }
}
