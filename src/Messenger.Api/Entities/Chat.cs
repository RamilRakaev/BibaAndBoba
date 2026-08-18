namespace Messenger.Api.Entities;

public class Chat
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
