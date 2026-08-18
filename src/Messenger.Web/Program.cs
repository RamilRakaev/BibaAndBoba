using Messenger.Web.Components;
using Messenger.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ApiSession>();
builder.Services.AddScoped(sp =>
{
    var session = sp.GetRequiredService<ApiSession>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = (configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8081").TrimEnd('/') + "/";
    var handler = new HttpClientHandler
    {
        CookieContainer = session.Cookies,
        UseCookies = true
    };

    return new HttpClient(handler)
    {
        BaseAddress = new Uri(baseUrl)
    };
});
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ApiAuthenticationStateProvider>());
builder.Services.AddScoped<ChatRealtimeService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
