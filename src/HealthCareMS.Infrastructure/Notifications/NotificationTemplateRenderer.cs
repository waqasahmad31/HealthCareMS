using HealthCareMS.Domain.Appointments;

namespace HealthCareMS.Infrastructure.Notifications;

internal sealed record NotificationTemplate(string Subject, string Body);

internal static class NotificationTemplateRenderer
{
    public static NotificationTemplate AppointmentBooked(Appointment appointment)
    {
        var body = $"""
        Appointment {appointment.AppointmentNumber} has been booked.
        Doctor: {appointment.Doctor.User.FullName}
        Patient: {appointment.Patient.FirstName} {appointment.Patient.LastName}
        Time: {appointment.ScheduledAt:yyyy-MM-dd HH:mm} UTC
        Type: {appointment.Type}
        """;

        return new NotificationTemplate(
            $"Appointment booked: {appointment.AppointmentNumber}",
            body);
    }

    public static NotificationTemplate AppointmentReminder(Appointment appointment, string reminderWindow)
    {
        var body = $"""
        Reminder: Appointment {appointment.AppointmentNumber} is scheduled at {appointment.ScheduledAt:yyyy-MM-dd HH:mm} UTC.
        Doctor: {appointment.Doctor.User.FullName}
        Type: {appointment.Type}
        """;

        return new NotificationTemplate(
            $"Appointment reminder ({reminderWindow}): {appointment.AppointmentNumber}",
            body);
    }
}
