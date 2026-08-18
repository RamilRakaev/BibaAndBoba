using Messenger.Api.DTOs;
using Messenger.Api.Entities;
using Messenger.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/chats")]
public class ChatsController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly MessageService _messageService;

    public ChatsController(ChatService chatService, MessageService messageService)
    {
        _chatService = chatService;
        _messageService = messageService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatDto>>> GetChats()
    {
        var chats = await _chatService.GetChatsAsync(User.GetUserId(), User.IsAdmin());
        return Ok(chats);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChatDto>> GetChat(Guid id)
    {
        var chat = await _chatService.GetChatAsync(id, User.GetUserId(), User.IsAdmin());
        return Ok(chat);
    }

    [HttpPost("start")]
    public async Task<ActionResult<ChatDto>> Start(StartChatRequest request)
    {
        var chat = await _chatService.StartChatAsync(User.GetUserId(), request);
        return Ok(chat);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _chatService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{chatId:guid}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(Guid chatId)
    {
        var messages = await _messageService.GetMessagesAsync(chatId, User.GetUserId(), User.IsAdmin());
        return Ok(messages);
    }

    [HttpPost("{chatId:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid chatId, SendMessageRequest request)
    {
        var message = await _messageService.SendAsync(chatId, User.GetUserId(), User.IsAdmin(), request);
        return Ok(message);
    }
}
