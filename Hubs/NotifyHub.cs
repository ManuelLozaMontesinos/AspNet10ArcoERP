using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AspNet10ArcoERP.Hubs
{
    public class NotifyHub : Hub
    {
        public async Task JoinArea(string areaName)
        {
            if (!string.IsNullOrEmpty(areaName))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Area_" + areaName);
            }
        }

        public async Task LeaveArea(string areaName)
        {
            if (!string.IsNullOrEmpty(areaName))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Area_" + areaName);
            }
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

