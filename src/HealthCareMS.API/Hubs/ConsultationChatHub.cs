using Microsoft.AspNetCore.SignalR;

namespace HealthCareMS.API.Hubs;

public sealed class ConsultationChatHub : Hub
{
    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sessionId));
    }

    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(sessionId));
    }

    public static string GroupName(Guid sessionId)
    {
        return $"ConsultationChat:{sessionId}";
    }
}
