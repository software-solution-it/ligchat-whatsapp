using Microsoft.AspNetCore.SignalR;

namespace WhatsAppProject.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinSectorGroup(string sectorId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sector_{sectorId}");
        }

        public async Task LeaveSectorGroup(string sectorId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sector_{sectorId}");
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
} 