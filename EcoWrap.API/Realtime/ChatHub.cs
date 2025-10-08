using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace EcoWrap.API.Realtime
{
    [Authorize]
    public class ChatHub : Hub
    {
        public Task JoinConversation(long conversationId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");

        public Task LeaveConversation(long conversationId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
    }
}
