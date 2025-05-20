using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArzanGo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly ILogger<WebSocketHandler> _logger;
    private static readonly ConcurrentDictionary<Guid, string> _userConnections = new();
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public WebSocketHandler(IDbContextFactory<AppDbContext> contextFactory, ILogger<WebSocketHandler> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, Guid? userId = null)
    {
        var clientId = Guid.NewGuid().ToString();
        _clients.TryAdd(clientId, webSocket);

        if (userId.HasValue)
        {
            _userConnections.TryAdd(userId.Value, clientId);
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

    private async Task SendAllOrdersAsync(WebSocket webSocket)
    {
        await using var context = _contextFactory.CreateDbContext();
        var orders = await context.Orders
            .Include(o => o.User)
            .Include(o => o.Address)
            .Include(o => o.PaymentSettings)
            .Include(o => o.OrderItems!)
            .ThenInclude(o=>o.Product)
            .ThenInclude(o => o!.ProductPhotos)
            .AsNoTracking()
            .ToListAsync();

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var ordersJson = JsonSerializer.Serialize(orders, options);
        var buffer = Encoding.UTF8.GetBytes(ordersJson);

        await webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }
    private async Task SendSingleOrderAsync(WebSocket webSocket, Guid orderId)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();

            // Получаем заказ и связанные данные
            var order = await context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.PaymentSettings)
                .Include(o => o.OrderItems!)
                .ThenInclude(o => o.Product)
                .ThenInclude(o => o!.ProductPhotos)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", orderId);
                await SendErrorMessageAsync(webSocket, "Order not found");
                return;
            }

            // Получаем всех курьеров (Users с Courier == true)
            var couriers = await context.Users
                .Where(u => u.Courier == true)
                .AsNoTracking()
                .ToListAsync();

            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // Исключаем циклические ссылки и ненужные данные
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Создаем объект для отправки, включающий заказ и список курьеров
            var responseData = new
            {
                type = "order_update",
                data = new
                {
                    order = order,
                    couriers = couriers.Select(c => new
                    {
                        c.UserId,
                        c.FirstName,
                        c.LastName,
                        c.PhoneNumber,
                        c.Email,
                        c.Raiting,
                        c.FcmToken
                        // Исключаем чувствительные данные как Password и другие ненужные поля
                    })
                }
            };

            var orderJson = JsonSerializer.Serialize(responseData, options);
            await SendJsonMessageAsync(webSocket, orderJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order {OrderId} to client", orderId);
            await SendErrorMessageAsync(webSocket, "Internal server error");
        }
    }

    private async Task SendJsonMessageAsync(WebSocket webSocket, string json)
    {
        var buffer = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    private async Task SendErrorMessageAsync(WebSocket webSocket, string error)
    {
        var errorMessage = JsonSerializer.Serialize(new { type = "error", message = error });
        await SendJsonMessageAsync(webSocket, errorMessage);
    }

    public async Task SendOrderUpdateAsync(Guid orderId)
    {
        var disconnectedClientIds = new List<string>();

        foreach (var client in _clients)
        {
            try
            {
                if (client.Value.State == WebSocketState.Open)
                {
                    await SendSingleOrderAsync(client.Value, orderId);
                }
                else
                {
                    disconnectedClientIds.Add(client.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order update to client {ClientId}", client.Key);
                disconnectedClientIds.Add(client.Key);
            }
        }

        foreach (var clientId in disconnectedClientIds)
        {
            _clients.TryRemove(clientId, out _);
            _logger.LogInformation("Client {ClientId} disconnected and removed", clientId);
        }
    }

    private async Task BroadcastMessageAsync(string message)
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