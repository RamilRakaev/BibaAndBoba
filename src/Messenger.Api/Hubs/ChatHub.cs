using Messenger.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Messenger.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;

    public ChatHub(ChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task JoinChat(Guid chatId)
    {
        await EnsureCanJoinAsync(chatId);
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
    }

    public async Task LeaveChat(Guid chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
    }

    private async Task EnsureCanJoinAsync(Guid chatId)
    {
        var user = Context.User ?? throw new HubException("Пользователь не авторизован.");
        var userId = user.GetUserId();
        var isAdmin = user.IsAdmin();
        var canAccess = await _chatService.CanAccessChatAsync(chatId, userId, isAdmin);
        if (!canAccess)
        {
            throw new HubException("Нет доступа к этому чату.");
        }
    }
}
