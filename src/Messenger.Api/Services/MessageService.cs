using Messenger.Api.Data;
using Messenger.Api.DTOs;
using Messenger.Api.Entities;
using Messenger.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Api.Services;

public class MessageService
{
    private readonly AppDbContext _db;
    private readonly ChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessageService(AppDbContext db, ChatService chatService, IHubContext<ChatHub> hubContext)
    {
        _db = db;
        _chatService = chatService;
        _hubContext = hubContext;
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid chatId, Guid currentUserId, bool isAdmin)
    {
        await EnsureChatAccessAsync(chatId, currentUserId, isAdmin);

        var messages = await _db.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(Map).ToList();
    }

    public async Task<MessageDto> SendAsync(Guid chatId, Guid currentUserId, bool isAdmin, SendMessageRequest request)
    {
        await EnsureChatAccessAsync(chatId, currentUserId, isAdmin);
        var text = NormalizeText(request.Text);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderUserId = currentUserId,
            Text = text,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await _db.Entry(message).Reference(m => m.Sender).LoadAsync();
        var dto = Map(message);
        await _hubContext.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", dto);
        return dto;
    }

    public async Task<MessageDto> UpdateAsync(Guid id, Guid currentUserId, bool isAdmin, UpdateMessageRequest request)
    {
        var message = await GetRequiredAsync(id);
        EnsureCanModify(message, currentUserId, isAdmin);
        if (message.IsDeleted)
        {
            throw new AppException("Нельзя редактировать удалённое сообщение.");
        }

        message.Text = NormalizeText(request.Text);
        message.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _db.Entry(message).Reference(m => m.Sender).LoadAsync();
        var dto = Map(message);
        await _hubContext.Clients.Group(message.ChatId.ToString()).SendAsync("MessageUpdated", dto);
        return dto;
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, bool isAdmin)
    {
        var message = await GetRequiredAsync(id);
        EnsureCanModify(message, currentUserId, isAdmin);
        message.IsDeleted = true;
        message.Text = string.Empty;
        await _db.SaveChangesAsync();

        await _hubContext.Clients.Group(message.ChatId.ToString()).SendAsync("MessageDeleted", message.Id);
    }

    public static MessageDto Map(Message message) => new()
    {
        Id = message.Id,
        ChatId = message.ChatId,
        SenderUserId = message.SenderUserId,
        SenderDisplayName = message.Sender?.DisplayName ?? message.Sender?.UserName ?? string.Empty,
        Text = message.IsDeleted ? string.Empty : message.Text,
        CreatedAt = message.CreatedAt,
        EditedAt = message.EditedAt,
        IsDeleted = message.IsDeleted
    };

    private async Task EnsureChatAccessAsync(Guid chatId, Guid currentUserId, bool isAdmin)
    {
        var canAccess = await _chatService.CanAccessChatAsync(chatId, currentUserId, isAdmin);
        if (!canAccess)
        {
            var exists = await _db.Chats.AnyAsync(c => c.Id == chatId);
            throw exists
                ? new AppException("Нет доступа к этому чату.", StatusCodes.Status403Forbidden)
                : new AppException("Чат не найден.", StatusCodes.Status404NotFound);
        }
    }

    private async Task<Message> GetRequiredAsync(Guid id)
    {
        var message = await _db.Messages.Include(m => m.Sender).FirstOrDefaultAsync(m => m.Id == id);
        if (message is null)
        {
            throw new AppException("Сообщение не найдено.", StatusCodes.Status404NotFound);
        }

        return message;
    }

    private static void EnsureCanModify(Message message, Guid currentUserId, bool isAdmin)
    {
        if (!isAdmin && message.SenderUserId != currentUserId)
        {
            throw new AppException("Недостаточно прав для изменения сообщения.", StatusCodes.Status403Forbidden);
        }
    }

    private static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AppException("Текст сообщения обязателен.");
        }

        var trimmed = text.Trim();
        if (trimmed.Length > 4000)
        {
            throw new AppException("Текст сообщения не должен превышать 4000 символов.");
        }

        return trimmed;
    }
}
