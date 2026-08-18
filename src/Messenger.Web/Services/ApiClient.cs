using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Messenger.Web.Models;

namespace Messenger.Web.Services;

public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<CurrentUserDto> LoginAsync(LoginRequest request) =>
        PostAsync<LoginRequest, CurrentUserDto>("api/auth/login", request);

    public async Task LogoutAsync()
    {
        using var response = await _http.PostAsync("api/auth/logout", null);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NoContent or HttpStatusCode.OK)
        {
            return;
        }

        await EnsureSuccess(response);
    }

    public async Task<CurrentUserDto?> GetMeAsync()
    {
        using var response = await _http.GetAsync("api/auth/me");
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<CurrentUserDto>(JsonOptions);
    }

    public Task<List<UserDto>> GetUsersAsync() =>
        GetAsync<List<UserDto>>("api/users");

    public Task<UserDto> CreateUserAsync(CreateUserRequest request) =>
        PostAsync<CreateUserRequest, UserDto>("api/users", request);

    public Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request) =>
        PutAsync<UpdateUserRequest, UserDto>($"api/users/{id}", request);

    public Task DeactivateUserAsync(Guid id) => DeleteAsync($"api/users/{id}");

    public Task ChangePasswordAsync(Guid id, ChangePasswordRequest request) =>
        PostAsync($"api/users/{id}/change-password", request);

    public Task<List<ChatDto>> GetChatsAsync() =>
        GetAsync<List<ChatDto>>("api/chats");

    public Task<ChatDto> GetChatAsync(Guid id) =>
        GetAsync<ChatDto>($"api/chats/{id}");

    public Task<ChatDto> StartChatAsync(Guid targetUserId) =>
        PostAsync<StartChatRequest, ChatDto>("api/chats/start", new StartChatRequest { TargetUserId = targetUserId });

    public Task<List<MessageDto>> GetMessagesAsync(Guid chatId) =>
        GetAsync<List<MessageDto>>($"api/chats/{chatId}/messages");

    public Task<MessageDto> SendMessageAsync(Guid chatId, string text) =>
        PostAsync<SendMessageRequest, MessageDto>($"api/chats/{chatId}/messages", new SendMessageRequest { Text = text });

    private async Task<T> GetAsync<T>(string url)
    {
        using var response = await _http.GetAsync(url);
        await EnsureSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return result ?? throw new InvalidOperationException("Пустой ответ API.");
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        using var response = await _http.PostAsJsonAsync(url, body);
        await EnsureSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        return result ?? throw new InvalidOperationException("Пустой ответ API.");
    }

    private async Task PostAsync<TRequest>(string url, TRequest body)
    {
        using var response = await _http.PostAsJsonAsync(url, body);
        await EnsureSuccess(response);
    }

    private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest body)
    {
        using var response = await _http.PutAsJsonAsync(url, body);
        await EnsureSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        return result ?? throw new InvalidOperationException("Пустой ответ API.");
    }

    private async Task DeleteAsync(string url)
    {
        using var response = await _http.DeleteAsync(url);
        await EnsureSuccess(response);
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Требуется авторизация.");
        }

        var payload = await response.Content.ReadAsStringAsync();
        var message = payload;
        try
        {
            var error = JsonSerializer.Deserialize<ApiError>(payload, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                message = error.Message;
            }
        }
        catch (JsonException)
        {
            // Keep raw payload.
        }

        throw new HttpRequestException(string.IsNullOrWhiteSpace(message)
            ? $"Ошибка API: {(int)response.StatusCode}"
            : message);
    }
}
