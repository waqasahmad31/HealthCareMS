using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Notifications;
using HealthCareMS.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(
    INotificationService notificationService,
    ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? userId,
        [FromQuery] bool unreadOnly,
        CancellationToken cancellationToken)
    {
        var resolvedUserId = ResolveUserId(userId);
        if (!resolvedUserId.HasValue)
        {
            return Fail(new Error("NOTIFICATION_USER_INVALID", "Authenticated user is required."));
        }

        var notifications = await notificationService.GetForUserAsync(resolvedUserId.Value, unreadOnly, cancellationToken);
        return OkEnvelope(notifications);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var resolvedUserId = ResolveUserId(userId);
        if (!resolvedUserId.HasValue)
        {
            return Fail(new Error("NOTIFICATION_USER_INVALID", "Authenticated user is required."));
        }

        var result = await notificationService.MarkAsReadAsync(id, resolvedUserId.Value, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences([FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var resolvedUserId = ResolveUserId(userId);
        if (!resolvedUserId.HasValue)
        {
            return Fail(new Error("NOTIFICATION_USER_INVALID", "Authenticated user is required."));
        }

        var result = await notificationService.GetPreferencesAsync(resolvedUserId.Value, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromQuery] Guid? userId,
        UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var resolvedUserId = ResolveUserId(userId);
        if (!resolvedUserId.HasValue)
        {
            return Fail(new Error("NOTIFICATION_USER_INVALID", "Authenticated user is required."));
        }

        var result = await notificationService.UpdatePreferencesAsync(resolvedUserId.Value, request, cancellationToken);
        return FromResult(result);
    }

    private Guid? ResolveUserId(Guid? requestedUserId)
    {
        return requestedUserId ?? currentUser.UserId;
    }
}
