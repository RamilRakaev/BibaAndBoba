using Messenger.Web;
using Messenger.Web.Components;
using Messenger.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Messenger.Web.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.LoginPath = "/login";
    });

builder.Services.AddAuthorization();
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
builder.Services.AddScoped<WebAuthService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
