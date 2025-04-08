using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

public class WebSocketHandler
{
    private static readonly ConcurrentBag<WebSocket> _clients = new();

    public async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        _clients.Add(webSocket);
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await BroadcastMessageAsync(message);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                _clients.TryTake(out _); // Удалить клиента из списка
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
        }
    }

    private static async Task BroadcastMessageAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var buffer = new ArraySegment<byte>(bytes);

        foreach (var client in _clients.ToList())
        {
            if (client.State == WebSocketState.Open)
            {
                await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
