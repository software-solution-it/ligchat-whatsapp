using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;

public class WebSocketManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, bool>> _clients = new ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, bool>>();

    public void AddClient(string sectorId, WebSocket webSocket)
    {
        var clientDict = _clients.GetOrAdd(sectorId, _ => new ConcurrentDictionary<WebSocket, bool>());

        foreach (var client in clientDict.Keys)
        {
            clientDict.TryRemove(client, out _);
        }

        clientDict.TryAdd(webSocket, true);
    }

    public async Task SendMessageToSectorAsync(string sectorId, string message)
    {
        if (_clients.TryGetValue(sectorId, out var clientDict))
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);

            var client = clientDict.Keys.FirstOrDefault();
            if (client != null && client.State == WebSocketState.Open)
            {
                await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    public void RemoveClient(WebSocket webSocket)
    {
        foreach (var entry in _clients)
        {
            var clientDict = entry.Value;
            if (clientDict.ContainsKey(webSocket))
            {
                if (clientDict.TryRemove(webSocket, out _))
                {
                    Console.WriteLine($"Client removed from sector {entry.Key}. Total clients in sector: {clientDict.Count}");
                }
                break; 
            }
        }
    }
}
