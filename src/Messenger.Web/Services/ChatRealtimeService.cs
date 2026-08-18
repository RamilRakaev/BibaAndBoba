using Messenger.Web.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Messenger.Web.Services;

public class ChatRealtimeService : IAsyncDisposable
{
    private readonly ApiSession _session;
    private readonly IConfiguration _configuration;
    private HubConnection? _connection;
    private Guid? _joinedChatId;

    public event Action<MessageDto>? MessageReceived;
    public event Action<MessageDto>? MessageUpdated;
    public event Action<Guid>? MessageDeleted;

    public ChatRealtimeService(ApiSession session, IConfiguration configuration)
    {
        _session = session;
        _configuration = configuration;
    }

    public async Task StartAsync()
    {
        if (_connection is { State: HubConnectionState.Connected or HubConnectionState.Connecting })
        {
            return;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        var baseUrl = (_configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8081").TrimEnd('/');
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/chat", options =>
            {
                options.Cookies = _session.Cookies;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MessageDto>("ReceiveMessage", message => MessageReceived?.Invoke(message));
        _connection.On<MessageDto>("MessageUpdated", message => MessageUpdated?.Invoke(message));
        _connection.On<Guid>("MessageDeleted", messageId => MessageDeleted?.Invoke(messageId));
        _connection.Reconnected += async _ =>
        {
            if (_joinedChatId.HasValue)
            {
                await _connection.InvokeAsync("JoinChat", _joinedChatId.Value);
            }
        };

        await _connection.StartAsync();
    }

    public async Task JoinChatAsync(Guid chatId)
    {
        await StartAsync();
        if (_connection is null)
        {
            return;
        }

        if (_joinedChatId.HasValue && _joinedChatId.Value != chatId)
        {
            await _connection.InvokeAsync("LeaveChat", _joinedChatId.Value);
        }

        await _connection.InvokeAsync("JoinChat", chatId);
        _joinedChatId = chatId;
    }

    public async Task LeaveChatAsync(Guid chatId)
    {
        if (_connection is null)
        {
            return;
        }

        try
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("LeaveChat", chatId);
            }
        }
        catch
        {
            // Ignore leave errors when disconnecting.
        }

        if (_joinedChatId == chatId)
        {
            _joinedChatId = null;
        }
    }

    public async Task StopAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _joinedChatId = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
