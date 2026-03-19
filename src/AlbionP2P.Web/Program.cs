using AlbionP2P.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<AlbionP2P.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001";

// ✅ CookieHandler: usa o fetch do browser com credentials:include
//    HttpClientHandler NÃO é suportado no WebAssembly (PlatformNotSupportedException)
builder.Services.AddTransient<CookieHandler>();

builder.Services
    .AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<DealService>();
builder.Services.AddScoped<ChatService>();

var host = builder.Build();

// Log para identificar erros na restauração de sessão na inicialização
var logger = host.Services.GetRequiredService<ILogger<Program>>();
try
{
    logger.LogInformation("Restaurando sessão do usuário...");
    var auth = host.Services.GetRequiredService<AuthService>();
    await auth.TryRestoreSessionAsync();
    logger.LogInformation("Sessão restaurada com sucesso.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Falha ao restaurar sessão: {Message}", ex.Message);
}

await host.RunAsync();
