using AlbionP2P.Web.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;

namespace AlbionP2P.Web.Services;

// ── Auth ──────────────────────────────────────────────────────────────────────
public class AuthService(HttpClient http)
{
    private UserDto? _user;
    public UserDto? CurrentUser     => _user;
    public bool     IsAuthenticated => _user is not null;
    public event Action? OnAuthChanged;

    public async Task<(bool ok, string? error)> RegisterAsync(RegisterRequest req)
    {
        try
        {
            var r = await (await http.PostAsJsonAsync("api/account/register", req))
                        .Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            if (r?.Success == true) { _user = r.Data; OnAuthChanged?.Invoke(); return (true, null); }
            return (false, r?.Error ?? "Erro ao registrar.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string? error)> LoginAsync(LoginRequest req)
    {
        try
        {
            var r = await (await http.PostAsJsonAsync("api/account/login", req))
                        .Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            if (r?.Success == true) { _user = r.Data; OnAuthChanged?.Invoke(); return (true, null); }
            return (false, r?.Error ?? "Email ou senha inválidos.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task LogoutAsync()
    {
        try { await http.PostAsync("api/account/logout", null); } catch { }
        _user = null; OnAuthChanged?.Invoke();
    }

    public async Task TryRestoreSessionAsync()
    {
        try
        {
            var r = await http.GetFromJsonAsync<ApiResponse<UserDto>>("api/account/me");
            if (r?.Success == true) { _user = r.Data; OnAuthChanged?.Invoke(); }
        }
        catch { }
    }
}

// ── Orders ────────────────────────────────────────────────────────────────────
public class OrderService(HttpClient http)
{
    public async Task<OrderListDto?> GetRecentAsync(ItemCategory? cat = null, OrderType? type = null, ServerRegion? region = null, int page = 1, int size = 20)
    {
        var q = $"api/orders?page={page}&pageSize={size}";
        if (cat.HasValue)    q += $"&category={cat}";
        if (type.HasValue)   q += $"&type={type}";
        if (region.HasValue) q += $"&region={region}";
        return (await http.GetFromJsonAsync<ApiResponse<OrderListDto>>(q))?.Data;
    }

    public async Task<List<OrderDto>?> GetMyOrdersAsync()
        => (await http.GetFromJsonAsync<ApiResponse<List<OrderDto>>>("api/orders/mine"))?.Data;

    public async Task<(bool ok, OrderDto? order, string? error)> CreateAsync(CreateOrderRequest req)
    {
        var r = await (await http.PostAsJsonAsync("api/orders", req)).Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        return r?.Success == true ? (true, r.Data, null) : (false, null, r?.Error ?? "Erro.");
    }

    public async Task<(bool ok, string? error)> CancelAsync(Guid id)
    {
        var r = await (await http.DeleteAsync($"api/orders/{id}")).Content.ReadFromJsonAsync<ApiResponse<object>>();
        return r?.Success == true ? (true, null) : (false, r?.Error);
    }
}

// ── Deals ─────────────────────────────────────────────────────────────────────
public class DealService(HttpClient http)
{
    public async Task<List<DealDto>?> GetMyDealsAsync()
        => (await http.GetFromJsonAsync<ApiResponse<List<DealDto>>>("api/deals/mine"))?.Data;

    public async Task<DealDto?> GetByIdAsync(Guid id)
        => (await http.GetFromJsonAsync<ApiResponse<DealDto>>($"api/deals/{id}"))?.Data;

    public async Task<(bool ok, DealDto? deal, string? error)> CreateAsync(CreateDealRequest req)
    {
        var r = await (await http.PostAsJsonAsync("api/deals", req)).Content.ReadFromJsonAsync<ApiResponse<DealDto>>();
        return r?.Success == true ? (true, r.Data, null) : (false, null, r?.Error ?? "Erro.");
    }

    public async Task<(bool ok, string? error)> AcceptBySeller(Guid id)
    {
        var r = await (await http.PostAsync($"api/deals/{id}/accept-seller", null)).Content.ReadFromJsonAsync<ApiResponse<object>>();
        return r?.Success == true ? (true, null) : (false, r?.Error);
    }

    public async Task<(bool ok, string? error)> AcceptByBuyer(Guid id)
    {
        var r = await (await http.PostAsync($"api/deals/{id}/accept-buyer", null)).Content.ReadFromJsonAsync<ApiResponse<object>>();
        return r?.Success == true ? (true, null) : (false, r?.Error);
    }

    public async Task<(bool ok, string? error)> RejectAsync(Guid id)
    {
        var r = await (await http.PostAsync($"api/deals/{id}/reject", null)).Content.ReadFromJsonAsync<ApiResponse<object>>();
        return r?.Success == true ? (true, null) : (false, r?.Error);
    }

    public async Task<(bool ok, string? error)> CompleteAsync(Guid id)
    {
        var r = await (await http.PostAsync($"api/deals/{id}/complete", null)).Content.ReadFromJsonAsync<ApiResponse<object>>();
        return r?.Success == true ? (true, null) : (false, r?.Error);
    }

    public async Task<(bool ok, string? error)> RateAsync(Guid dealId, int stars, string comment)
    {
        var req = new CreateRatingRequest { Stars = stars, Comment = comment };
        var r = await (await http.PostAsJsonAsync($"api/deals/{dealId}/rate", req)).Content.ReadFromJsonAsync<ApiResponse<object>>();
        return r?.Success == true ? (true, null) : (false, r?.Error);
    }

    public async Task<List<MessageDto>?> GetMessagesAsync(Guid id)
        => (await http.GetFromJsonAsync<ApiResponse<List<MessageDto>>>($"api/deals/{id}/messages"))?.Data;
}

// ── Chat SignalR ──────────────────────────────────────────────────────────────
public class ChatService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly string _hubUrl;
    public event Action<MessageDto>? OnMessageReceived;
    public event Action<string>?     OnError;
    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public ChatService(IConfiguration config)
        => _hubUrl = (config["ApiBaseUrl"] ?? "https://localhost:7001").TrimEnd('/') + "/hubs/chat";

    public async Task ConnectAsync()
    {
        // Já conectado ou conectando → não recria
        if (_hub?.State is HubConnectionState.Connected
                        or HubConnectionState.Connecting
                        or HubConnectionState.Reconnecting) return;

        // Hub existe mas está desconectado/com falha → descarta e recria
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
            _hub = null;
        }

        _hub = new HubConnectionBuilder()
            .WithUrl(_hubUrl, o =>
            {
                o.HttpMessageHandlerFactory = inner => new CookieHandler { InnerHandler = inner };
            })
            .WithAutomaticReconnect()
            .Build();
        _hub.On<MessageDto>("ReceiveMessage", m => OnMessageReceived?.Invoke(m));
        _hub.On<string>("Error", e => OnError?.Invoke(e));
        await _hub.StartAsync();
    }

    public async Task JoinDealAsync(Guid id)
    {
        if (_hub is null || _hub.State == HubConnectionState.Disconnected)
            await ConnectAsync();
        await _hub!.InvokeAsync("JoinDeal", id.ToString());
    }

    public async Task LeaveDealAsync(Guid id)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("LeaveDeal", id.ToString());
    }

    public async Task SendMessageAsync(Guid id, string content)
    {
        if (_hub?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Chat desconectado. Aguarde a reconexão.");
        await _hub.InvokeAsync("SendMessage", id.ToString(), content);
    }

    public async ValueTask DisposeAsync() { if (_hub is not null) await _hub.DisposeAsync(); }
}
