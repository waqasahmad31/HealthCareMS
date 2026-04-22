using Microsoft.AspNetCore.SignalR;

namespace HealthCareMS.API.Hubs;

public sealed class QueueHub : Hub
{
    public static string QueueGroup(Guid doctorId, DateOnly date)
    {
        return $"Queue:{doctorId:N}:{date:yyyyMMdd}";
    }

    public static string PatientGroup(Guid appointmentId)
    {
        return $"PatientQueue:{appointmentId:N}";
    }

    public async Task JoinDoctorQueue(Guid doctorId, string date)
    {
        if (DateOnly.TryParse(date, out var parsedDate))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, QueueGroup(doctorId, parsedDate));
        }
    }

    public async Task LeaveDoctorQueue(Guid doctorId, string date)
    {
        if (DateOnly.TryParse(date, out var parsedDate))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, QueueGroup(doctorId, parsedDate));
        }
    }

    public Task JoinPatientQueue(Guid appointmentId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, PatientGroup(appointmentId));
    }
}
