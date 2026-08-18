using Messenger.Api.Data;
using Messenger.Api.DTOs;
using Messenger.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Api.Services;

public class ChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatDto>> GetChatsAsync(Guid currentUserId, bool isAdmin)
    {
        var query = _db.Chats
            .AsNoTracking()
            .Include(c => c.Members)
            .ThenInclude(m => m.User)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(c => c.Members.Any(m => m.UserId == currentUserId));
        }

        var chats = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        var chatIds = chats.Select(c => c.Id).ToList();

        var lastMessages = await _db.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => chatIds.Contains(m.ChatId) && !m.IsDeleted)
            .ToListAsync();

        var lastByChat = lastMessages
            .GroupBy(m => m.ChatId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.CreatedAt).First());

        return chats
            .Select(chat => Map(chat, currentUserId, lastByChat.GetValueOrDefault(chat.Id)))
            .OrderByDescending(c => c.LastMessage?.CreatedAt ?? c.CreatedAt)
            .ToList();
    }

    public async Task<ChatDto> GetChatAsync(Guid chatId, Guid currentUserId, bool isAdmin)
    {
        var chat = await _db.Chats
            .AsNoTracking()
            .Include(c => c.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == chatId);

        if (chat is null)
        {
            throw new AppException("Чат не найден.", StatusCodes.Status404NotFound);
        }

        EnsureCanAccess(chat, currentUserId, isAdmin);
        var lastMessage = await _db.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.ChatId == chatId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        return Map(chat, currentUserId, lastMessage);
    }

    public async Task<ChatDto> StartChatAsync(Guid currentUserId, StartChatRequest request)
    {
        if (request.TargetUserId == Guid.Empty)
        {
            throw new AppException("targetUserId обязателен.");
        }

        if (request.TargetUserId == currentUserId)
        {
            throw new AppException("Нельзя начать чат с самим собой.");
        }

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.TargetUserId);
        if (target is null || !target.IsActive)
        {
            throw new AppException("Пользователь не найден или неактивен.", StatusCodes.Status400BadRequest);
        }

        var currentExists = await _db.Users.AnyAsync(u => u.Id == currentUserId && u.IsActive);
        if (!currentExists)
        {
            throw new AppException("Текущий пользователь неактивен.", StatusCodes.Status403Forbidden);
        }

        var existingChatId = await _db.ChatMembers
            .Where(m => m.UserId == currentUserId)
            .Select(m => m.ChatId)
            .Intersect(_db.ChatMembers.Where(m => m.UserId == request.TargetUserId).Select(m => m.ChatId))
            .FirstOrDefaultAsync();

        if (existingChatId != Guid.Empty)
        {
            return await GetChatAsync(existingChatId, currentUserId, isAdmin: true);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var duplicateChatId = await _db.ChatMembers
            .Where(m => m.UserId == currentUserId)
            .Select(m => m.ChatId)
            .Intersect(_db.ChatMembers.Where(m => m.UserId == request.TargetUserId).Select(m => m.ChatId))
            .FirstOrDefaultAsync();

        if (duplicateChatId != Guid.Empty)
        {
            await transaction.CommitAsync();
            return await GetChatAsync(duplicateChatId, currentUserId, isAdmin: true);
        }

        var now = DateTime.UtcNow;
        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            CreatedAt = now
        };

        chat.Members.Add(new ChatMember
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            UserId = currentUserId,
            JoinedAt = now
        });
        chat.Members.Add(new ChatMember
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            UserId = request.TargetUserId,
            JoinedAt = now
        });

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetChatAsync(chat.Id, currentUserId, isAdmin: true);
    }

    public async Task DeleteAsync(Guid chatId)
    {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
        if (chat is null)
        {
            throw new AppException("Чат не найден.", StatusCodes.Status404NotFound);
        }

        _db.Chats.Remove(chat);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> CanAccessChatAsync(Guid chatId, Guid userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _db.Chats.AnyAsync(c => c.Id == chatId);
        }

        return await _db.ChatMembers.AnyAsync(m => m.ChatId == chatId && m.UserId == userId);
    }

    private static void EnsureCanAccess(Chat chat, Guid currentUserId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (chat.Members.All(m => m.UserId != currentUserId))
        {
            throw new AppException("Нет доступа к этому чату.", StatusCodes.Status403Forbidden);
        }
    }

    private static ChatDto Map(Chat chat, Guid currentUserId, Message? lastMessage)
    {
        var participants = chat.Members
            .Select(m => UserService.Map(m.User))
            .ToList();

        return new ChatDto
        {
            Id = chat.Id,
            CreatedAt = chat.CreatedAt,
            Participants = participants,
            OtherUser = participants.FirstOrDefault(p => p.Id != currentUserId),
            LastMessage = lastMessage is null ? null : MessageService.Map(lastMessage)
        };
    }
}
