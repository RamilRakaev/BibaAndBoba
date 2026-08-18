using System.ComponentModel.DataAnnotations;

namespace Messenger.Api.DTOs;

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
    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;
}

public class UpdateMessageRequest
{
    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;
}
