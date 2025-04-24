using ArzanGo.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private static readonly ConcurrentDictionary<Guid, string> _userConnections = new(); // Для связи userId → WebSocket
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public WebSocketHandler(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, Guid? userId = null)
    {
        var clientId = Guid.NewGuid().ToString();
        _clients.TryAdd(clientId, webSocket);

        if (userId.HasValue)
        {
            _userConnections.TryAdd(userId.Value, clientId); // Связываем userId с WebSocket
        }

        try
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (message == "getOrders")
                    {
                        await SendAllOrdersAsync(webSocket);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _clients.TryRemove(clientId, out _);
                    if (userId.HasValue)
                    {
                        _userConnections.TryRemove(userId.Value, out _);
                    }
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
            }
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            if (userId.HasValue)
            {
                _userConnections.TryRemove(userId.Value, out _);
            }
        }
    }

    // Отправка списка заказов (как было)
    private async Task SendAllOrdersAsync(WebSocket webSocket)
    {
        await using var context = _contextFactory.CreateDbContext();
        var orders = await context.Orders
            .Include(o => o.Users)
            .Include(o => o.Address)
            .Include(o => o.PaymentSettings)
            .Include(o => o.OrderItems)
            .ToListAsync();

        var ordersJson = JsonSerializer.Serialize(orders);
        var buffer = Encoding.UTF8.GetBytes(ordersJson);

        await webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    // Рассылка обновлений заказов (как было)
    public async Task BroadcastOrdersUpdateAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        var orders = await context.Orders
            .Include(o => o.Users)
            .Include(o => o.Address)
            .Include(o => o.PaymentSettings)
            .Include(o => o.OrderItems)
            .ToListAsync();

        var ordersJson = JsonSerializer.Serialize(orders);
        await BroadcastMessageAsync(ordersJson);
    }

    // Отправка сообщения всем клиентам
    private static async Task BroadcastMessageAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var buffer = new ArraySegment<byte>(bytes);

        foreach (var client in _clients.Values.ToList())
        {
            if (client.State == WebSocketState.Open)
            {
                await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    // 🔥 Новый метод: Отправка уведомления конкретному пользователю
    public async Task SendNotificationToUserAsync(Guid userId, string message)
    {
        if (_userConnections.TryGetValue(userId, out var clientId) &&
            _clients.TryGetValue(clientId, out var webSocket) &&
            webSocket.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }
}