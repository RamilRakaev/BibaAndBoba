using Messenger.Api.DTOs;
using Messenger.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly MessageService _messageService;

    public MessagesController(MessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MessageDto>> Update(Guid id, UpdateMessageRequest request)
    {
        var message = await _messageService.UpdateAsync(id, User.GetUserId(), User.IsAdmin(), request);
        return Ok(message);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _messageService.DeleteAsync(id, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }
}
