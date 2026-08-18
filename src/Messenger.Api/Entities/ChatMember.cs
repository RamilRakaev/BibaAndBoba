namespace Messenger.Api.Entities;

public class ChatMember
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }

    public Chat Chat { get; set; } = null!;
    public User User { get; set; } = null!;
}
