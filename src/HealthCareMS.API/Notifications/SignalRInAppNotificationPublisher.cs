using HealthCareMS.API.Hubs;
using HealthCareMS.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace HealthCareMS.API.Notifications;

public sealed class SignalRInAppNotificationPublisher(IHubContext<NotificationHub> hubContext) : IInAppNotificationPublisher
{
    public Task PublishAsync(NotificationResponse notification, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(NotificationHub.UserGroup(notification.RecipientUserId))
            .SendAsync("NotificationReceived", notification, cancellationToken);
    }
}

