using ArzanGo.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public WebSocketHandler(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        var clientId = Guid.NewGuid().ToString();
        _clients.TryAdd(clientId, webSocket);

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
                    else
                    {
                        await BroadcastMessageAsync(message);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _clients.TryRemove(clientId, out _);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
            }
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
        }
    }

    private async Task SendAllOrdersAsync(WebSocket webSocket)
    {
        await using var context = _contextFactory.CreateDbContext();
        var orders = await context.Orders
            .Include(o => o.Users)
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

    public async Task BroadcastOrdersUpdateAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        var orders = await context.Orders
            .Include(o => o.Users)
            .Include(o => o.OrderItems)
            .ToListAsync();

        var ordersJson = JsonSerializer.Serialize(orders);
        var buffer = Encoding.UTF8.GetBytes(ordersJson);
        var segment = new ArraySegment<byte>(buffer);

        foreach (var client in _clients.Values.ToList())
        {
            if (client.State == WebSocketState.Open)
            {
                await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

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
}