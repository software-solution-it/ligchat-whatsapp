using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace WhatsAppProject.Services
{
    public class WebSocketConnectionHandler
    {
        private readonly ILogger<WebSocketConnectionHandler> _logger;

        public WebSocketConnectionHandler(ILogger<WebSocketConnectionHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleConnection(
            HttpContext context,
            WebSocket webSocket,
            WebSocketManager webSocketManager,
            string sectorId)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult? result = null;

            try
            {
                do
                {
                    result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(
                            buffer,
                            0,
                            result.Count);
                        
                        _logger.LogInformation($"Received message from sector {sectorId}: {message}");

                        // Envia a mensagem para todos os clientes do mesmo setor
                        await webSocketManager.SendMessageToSectorAsync(sectorId, message);
                    }
                }
                while (!result.CloseStatus.HasValue);
            }
            catch (WebSocketException ex)
            {
                _logger.LogError($"WebSocket error for sector {sectorId}: {ex.Message}");
            }
        }
    }
} 