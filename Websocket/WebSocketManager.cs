using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

public class WebSocketManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, bool>> _clients = new();
    private readonly ILogger<WebSocketManager> _logger;

    public WebSocketManager(ILogger<WebSocketManager> logger)
    {
        _logger = logger;
    }

    public void AddClient(string sectorId, WebSocket webSocket)
    {
        try
        {
            var clientDict = _clients.GetOrAdd(sectorId, _ => new ConcurrentDictionary<WebSocket, bool>());
            clientDict.TryAdd(webSocket, true);
            _logger.LogInformation($"Client added to sector {sectorId}. Total clients in sector: {clientDict.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding client to sector {sectorId}: {ex.Message}");
        }
    }

    public async Task SendMessageToSectorAsync(string sectorId, object messageObject)
    {
        try
        {
            _logger.LogInformation($"Iniciando envio de mensagem para setor {sectorId}");

            // Configuração específica para serialização
            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            // Serializar a mensagem
            var messageJson = JsonSerializer.Serialize(messageObject, options);
            
            _logger.LogInformation($"Mensagem serializada: {messageJson}");

            if (!_clients.TryGetValue(sectorId, out var clientDict))
            {
                _logger.LogWarning($"Nenhum cliente encontrado para o setor {sectorId}");
                return;
            }

            var buffer = Encoding.UTF8.GetBytes(messageJson);
            var segment = new ArraySegment<byte>(buffer);
            var deadSockets = new List<WebSocket>();

            foreach (var client in clientDict.Keys)
            {
                try
                {
                    if (client.State == WebSocketState.Open)
                    {
                        await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                        _logger.LogInformation($"Mensagem enviada com sucesso para cliente");
                    }
                    else
                    {
                        _logger.LogWarning($"Cliente no estado {client.State}. Removendo...");
                        deadSockets.Add(client);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erro ao enviar mensagem para cliente: {ex.Message}");
                    deadSockets.Add(client);
                }
            }

            foreach (var socket in deadSockets)
            {
                RemoveClient(socket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao enviar mensagem para setor {sectorId}: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    public void RemoveClient(WebSocket webSocket)
    {
        foreach (var entry in _clients)
        {
            var clientDict = entry.Value;
            if (clientDict.TryRemove(webSocket, out _))
            {
                _logger.LogInformation($"Client removed from sector {entry.Key}. Total clients in sector: {clientDict.Count}");
            }
        }
    }
}
