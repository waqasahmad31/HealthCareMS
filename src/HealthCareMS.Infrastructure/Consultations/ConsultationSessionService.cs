using System.Security.Cryptography;
using System.Text;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Infrastructure.Configuration;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Infrastructure.Consultations;

public sealed class ConsultationSessionService(
    HealthCareDbContext dbContext,
    IOptions<ConsultationSessionOptions> options,
    IApplicationLinkBuilder? applicationLinkBuilder = null,
    IConsultationSessionNotifier? notifier = null) : IConsultationSessionService
{
    private readonly ConsultationSessionOptions options = options.Value;
    private readonly IApplicationLinkBuilder? applicationLinkBuilder = applicationLinkBuilder;

    public async Task<Result<ConsultationSessionResponse>> StartAsync(
        StartConsultationSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId == Guid.Empty)
        {
            return Result<ConsultationSessionResponse>.Failure(Error.Validation([
                new ValidationError(nameof(request.AppointmentId), "AppointmentId is required.")
            ]));
        }

        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<ConsultationSessionResponse>.Failure(new Error("SESSION_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        var validation = ValidateOnlineAppointment(appointment);
        if (validation.IsFailure)
        {
            return Result<ConsultationSessionResponse>.Failure(validation.Error);
        }

        var existing = await SessionQuery()
            .SingleOrDefaultAsync(x => x.AppointmentId == request.AppointmentId, cancellationToken);

        if (existing is not null)
        {
            return Result<ConsultationSessionResponse>.Success(Map(existing));
        }

        var meetingLink = BuildMeetingLink(appointment.Id);
        appointment.MeetingLink = meetingLink;

        var session = new ConsultationSession
        {
            Appointment = appointment,
            AppointmentId = appointment.Id,
            Patient = appointment.Patient,
            PatientId = appointment.PatientId,
            Doctor = appointment.Doctor,
            DoctorId = appointment.DoctorId,
            ChannelName = BuildChannelName(appointment.Id),
            MeetingLink = meetingLink,
            Status = ConsultationSessionStatus.Waiting,
            LastTokenIssuedAt = DateTimeOffset.UtcNow
        };

        dbContext.ConsultationSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = Map(session);
        if (notifier is not null)
        {
            await notifier.NotifySessionChangedAsync(response, "SessionStarted", cancellationToken);
        }

        return Result<ConsultationSessionResponse>.Success(response);
    }

    public async Task<Result<ConsultationSessionResponse>> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await SessionQuery()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        return session is null
            ? Result<ConsultationSessionResponse>.Failure(new Error("SESSION_NOT_FOUND", "Consultation session was not found."))
            : Result<ConsultationSessionResponse>.Success(Map(session));
    }

    public async Task<Result<ConsultationSessionResponse>> GetByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var session = await SessionQuery()
            .SingleOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);

        return session is null
            ? Result<ConsultationSessionResponse>.Failure(new Error("SESSION_NOT_FOUND", "Consultation session was not found."))
            : Result<ConsultationSessionResponse>.Success(Map(session));
    }

    public async Task<Result<JoinConsultationSessionResponse>> JoinAsync(
        Guid sessionId,
        JoinConsultationSessionRequest request,
        CancellationToken cancellationToken)
    {
        var participant = NormalizeParticipant(request.ParticipantType);
        if (participant is null)
        {
            return Result<JoinConsultationSessionResponse>.Failure(new Error("SESSION_PARTICIPANT_INVALID", "ParticipantType must be Patient or Doctor."));
        }

        var session = await SessionQuery()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return Result<JoinConsultationSessionResponse>.Failure(new Error("SESSION_NOT_FOUND", "Consultation session was not found."));
        }

        var validation = ValidateOnlineAppointment(session.Appointment);
        if (validation.IsFailure)
        {
            return Result<JoinConsultationSessionResponse>.Failure(validation.Error);
        }

        var now = DateTimeOffset.UtcNow;
        if (participant == "Patient")
        {
            session.PatientJoinedAt ??= now;
        }
        else
        {
            session.DoctorJoinedAt ??= now;
        }

        if (session.PatientJoinedAt.HasValue && session.DoctorJoinedAt.HasValue)
        {
            session.Status = ConsultationSessionStatus.InProgress;
            session.StartedAt ??= now;
            if (session.Appointment.Status == AppointmentStatus.Confirmed)
            {
                session.Appointment.Status = AppointmentStatus.InProgress;
            }
        }

        session.LastTokenIssuedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        var expiresAt = now.AddMinutes(Math.Clamp(options.TokenExpiryMinutes, 5, 240));
        var uid = BuildUid(session, participant);
        var token = GenerateToken(session.ChannelName, uid, expiresAt);
        var response = new JoinConsultationSessionResponse(
            Map(session),
            participant,
            string.IsNullOrWhiteSpace(options.AppId) ? "local-dev-app-id" : options.AppId,
            session.ChannelName,
            token,
            uid,
            expiresAt,
            session.PatientJoinedAt.HasValue,
            session.DoctorJoinedAt.HasValue);

        if (notifier is not null)
        {
            await notifier.NotifySessionChangedAsync(response.Session, $"{participant}Joined", cancellationToken);
        }

        return Result<JoinConsultationSessionResponse>.Success(response);
    }

    private IQueryable<Appointment> AppointmentQuery()
    {
        return dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User);
    }

    private IQueryable<ConsultationSession> SessionQuery()
    {
        return dbContext.ConsultationSessions
            .Include(x => x.Appointment)
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User);
    }

    private static Result ValidateOnlineAppointment(Appointment appointment)
    {
        if (appointment.Type != AppointmentType.Online)
        {
            return Result.Failure(new Error("SESSION_APPOINTMENT_TYPE_INVALID", "Only online appointments can start a video consultation session."));
        }

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Completed)
        {
            return Result.Failure(new Error("SESSION_APPOINTMENT_STATUS_INVALID", "Cancelled, no-show, or completed appointments cannot join a video session."));
        }

        if (appointment.Status == AppointmentStatus.Pending)
        {
            return Result.Failure(new Error("SESSION_APPOINTMENT_NOT_CONFIRMED", "Appointment must be confirmed before starting a video session."));
        }

        return Result.Success();
    }

    private string BuildMeetingLink(Guid appointmentId)
    {
        if (applicationLinkBuilder is not null)
        {
            return applicationLinkBuilder.BuildConsultationWaitingRoomUrl(appointmentId);
        }

        if (string.IsNullOrWhiteSpace(options.ClientBaseUrl))
        {
            throw new InvalidOperationException("Agora:ClientBaseUrl or ApplicationLinks:ClientBaseUrl configuration is required.");
        }

        var baseUrl = options.ClientBaseUrl.TrimEnd('/');

        return $"{baseUrl}/consultation/waiting-room/{appointmentId}";
    }

    private static string BuildChannelName(Guid appointmentId)
    {
        return $"consultation-{appointmentId:N}";
    }

    private string GenerateToken(string channelName, int uid, DateTimeOffset expiresAt)
    {
        var key = string.IsNullOrWhiteSpace(options.AppCertificate)
            ? "local-dev-app-certificate"
            : options.AppCertificate;
        var raw = $"{options.AppId}|{channelName}|{uid}|{expiresAt.ToUnixTimeSeconds()}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{raw}|{signature}"));
    }

    private static int BuildUid(ConsultationSession session, string participant)
    {
        var bytes = participant == "Patient"
            ? session.PatientId.ToByteArray()
            : session.DoctorId.ToByteArray();

        return Math.Abs(BitConverter.ToInt32(bytes, 0)) + 1;
    }

    private static string? NormalizeParticipant(string participantType)
    {
        if (string.Equals(participantType, "Patient", StringComparison.OrdinalIgnoreCase))
        {
            return "Patient";
        }

        if (string.Equals(participantType, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            return "Doctor";
        }

        return null;
    }

    private static ConsultationSessionResponse Map(ConsultationSession session)
    {
        return new ConsultationSessionResponse(
            session.Id,
            session.AppointmentId,
            session.Appointment.AppointmentNumber,
            session.PatientId,
            $"{session.Patient.FirstName} {session.Patient.LastName}".Trim(),
            session.DoctorId,
            session.Doctor.User.FullName,
            session.ChannelName,
            session.MeetingLink,
            session.Status.ToString(),
            session.PatientJoinedAt,
            session.DoctorJoinedAt,
            session.StartedAt,
            session.EndedAt,
            session.CreatedAt);
    }
}
