using Microsoft.AspNetCore.SignalR;

namespace HealthCareMS.API.Hubs;

public sealed class NotificationHub : Hub
{
    public static string UserGroup(Guid userId)
    {
        return $"Notifications:{userId:N}";
    }

    public Task JoinUserNotifications(Guid userId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
    }
}

