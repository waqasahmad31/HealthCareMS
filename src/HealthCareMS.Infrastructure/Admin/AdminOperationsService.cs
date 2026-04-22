using HealthCareMS.Application.Admin;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Admin;

public sealed class AdminOperationsService(HealthCareDbContext dbContext) : IAdminOperationsService
{
    private static readonly SystemSetting[] DefaultSettings =
    [
        Setting("Platform.TimeZone", "Platform", "Time zone", "Asia/Karachi", "String", "Default platform time zone."),
        Setting("Platform.DefaultCurrency", "Platform", "Default currency", "PKR", "String", "Currency used for invoices and reports."),
        Setting("Platform.SupportEmail", "Platform", "Support email", "support@healthcarems.local", "String", "Public support email address."),
        Setting("Security.MaintenanceMode", "Security", "Maintenance mode", "false", "Boolean", "Temporarily limit platform access."),
        Setting("Notifications.EmailEnabled", "Notifications", "Email enabled", "true", "Boolean", "Allow outgoing email notifications."),
        Setting("Notifications.SmsEnabled", "Notifications", "SMS enabled", "false", "Boolean", "Allow outgoing SMS notifications."),
        Setting("Appointments.Reminder24HrEnabled", "Appointments", "24hr reminders", "true", "Boolean", "Schedule appointment reminders 24 hours before visit."),
        Setting("Appointments.Reminder2HrEnabled", "Appointments", "2hr reminders", "true", "Boolean", "Schedule appointment reminders 2 hours before visit."),
        Setting("Performance.SlowQueryThresholdMs", "Performance", "Slow query threshold", "500", "Number", "Threshold used by admins during performance reviews.")
    ];

    public async Task<AdminAppointmentOverviewResponse> GetAppointmentOverviewAsync(
        Guid? patientId,
        Guid? doctorId,
        string? status,
        DateOnly? date,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = AppointmentQuery();

        if (patientId.HasValue)
        {
            query = query.Where(x => x.PatientId == patientId.Value);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(x => x.DoctorId == doctorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (date.HasValue)
        {
            var start = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = start.AddDays(1);
            query = query.Where(x => x.ScheduledAt >= start && x.ScheduledAt < end);
        }

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 10, 200);

        var totalCount = await query.CountAsync(cancellationToken);
        var counts = await query
            .GroupBy(x => x.Status)
            .Select(x => new AppointmentStatusCount(x.Key, x.Count()))
            .ToListAsync(cancellationToken);
        var completedFeeTotal = await query
            .Where(x => x.Status == AppointmentStatus.Completed)
            .SumAsync(x => x.ConsultationFee, cancellationToken);
        var appointments = await query
            .OrderByDescending(x => x.ScheduledAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new AdminAppointmentOverviewResponse(
            totalCount,
            pageNumber,
            pageSize,
            Count(counts, AppointmentStatus.Pending),
            Count(counts, AppointmentStatus.Confirmed),
            Count(counts, AppointmentStatus.InProgress),
            Count(counts, AppointmentStatus.Completed),
            Count(counts, AppointmentStatus.Cancelled),
            Count(counts, AppointmentStatus.NoShow),
            completedFeeTotal,
            appointments.Select(MapAppointment).ToList());
    }

    public async Task<AdminDoctorManagementResponse> GetDoctorManagementAsync(
        string? specialization,
        string? city,
        bool? isVerified,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = DoctorQuery();

        if (!string.IsNullOrWhiteSpace(specialization))
        {
            var value = specialization.Trim().ToLowerInvariant();
            query = query.Where(x => x.Specialization.ToLower().Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var value = city.Trim().ToLowerInvariant();
            query = query.Where(x => x.City.ToLower().Contains(value));
        }

        if (isVerified.HasValue)
        {
            query = query.Where(x => x.IsVerified == isVerified.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var verifiedCount = await query.CountAsync(x => x.IsVerified, cancellationToken);
        var activeCount = await query.CountAsync(x => x.IsActive, cancellationToken);
        var doctors = await query
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .Take(200)
            .ToListAsync(cancellationToken);

        return new AdminDoctorManagementResponse(
            totalCount,
            verifiedCount,
            totalCount - verifiedCount,
            activeCount,
            totalCount - activeCount,
            doctors.Select(MapDoctor).ToList());
    }

    public async Task<Result<AdminDoctorRowResponse>> SetDoctorStatusAsync(
        Guid doctorId,
        UpdateDoctorAdminStatusRequest request,
        CancellationToken cancellationToken)
    {
        var doctor = await DoctorQuery()
            .SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);

        if (doctor is null)
        {
            return Result<AdminDoctorRowResponse>.Failure(new Error("ADMIN_DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        doctor.IsVerified = request.IsVerified;
        doctor.IsActive = request.IsActive;
        doctor.User.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AdminDoctorRowResponse>.Success(MapDoctor(doctor));
    }

    public async Task<SystemConfigurationResponse> GetSystemConfigurationAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultSettingsAsync(cancellationToken);

        var settings = await dbContext.SystemSettings
            .AsNoTracking()
            .OrderBy(x => x.GroupName)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return new SystemConfigurationResponse(settings.Select(MapSetting).ToList());
    }

    public async Task<Result<SystemSettingResponse>> UpdateSystemSettingAsync(
        string settingKey,
        UpdateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureDefaultSettingsAsync(cancellationToken);

        var normalizedKey = settingKey.Trim();
        var setting = await dbContext.SystemSettings
            .SingleOrDefaultAsync(x => x.SettingKey == normalizedKey, cancellationToken);

        if (setting is null)
        {
            return Result<SystemSettingResponse>.Failure(new Error("ADMIN_SETTING_NOT_FOUND", "System setting was not found."));
        }

        if (!setting.IsEditable)
        {
            return Result<SystemSettingResponse>.Failure(new Error("ADMIN_SETTING_READONLY", "System setting is read-only."));
        }

        var validation = ValidateSettingValue(setting, request.Value);
        if (validation.IsFailure)
        {
            return Result<SystemSettingResponse>.Failure(validation.Error);
        }

        setting.Value = request.Value.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<SystemSettingResponse>.Success(MapSetting(setting));
    }

    private IQueryable<Appointment> AppointmentQuery()
    {
        return dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User);
    }

    private IQueryable<Doctor> DoctorQuery()
    {
        return dbContext.Doctors
            .Include(x => x.User)
            .Include(x => x.Schedules);
    }

    private async Task EnsureDefaultSettingsAsync(CancellationToken cancellationToken)
    {
        var existingKeys = await dbContext.SystemSettings
            .Select(x => x.SettingKey)
            .ToListAsync(cancellationToken);
        var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = DefaultSettings
            .Where(x => !existing.Contains(x.SettingKey))
            .Select(CloneSetting)
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        dbContext.SystemSettings.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Result ValidateSettingValue(SystemSetting setting, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure(Error.Validation([new ValidationError(nameof(value), "Value is required.")]));
        }

        return setting.ValueType switch
        {
            "Boolean" when !bool.TryParse(value, out _) => Result.Failure(new Error("ADMIN_SETTING_VALUE_INVALID", "Value must be true or false.")),
            "Number" when !decimal.TryParse(value, out _) => Result.Failure(new Error("ADMIN_SETTING_VALUE_INVALID", "Value must be numeric.")),
            _ => Result.Success()
        };
    }

    private static int Count(IEnumerable<AppointmentStatusCount> counts, AppointmentStatus status)
    {
        foreach (var item in counts)
        {
            if (item.Status == status)
            {
                return item.Count;
            }
        }

        return 0;
    }

    private static AdminAppointmentRowResponse MapAppointment(Appointment appointment)
    {
        return new AdminAppointmentRowResponse(
            appointment.Id,
            appointment.AppointmentNumber,
            appointment.PatientId,
            $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim(),
            appointment.DoctorId,
            appointment.Doctor.User.FullName,
            appointment.ScheduledAt,
            appointment.Type.ToString(),
            appointment.Status.ToString(),
            appointment.Priority.ToString(),
            appointment.ReasonForVisit,
            appointment.ConsultationFee,
            appointment.PaymentStatus.ToString(),
            appointment.QueueNumber,
            appointment.CreatedAt);
    }

    private static AdminDoctorRowResponse MapDoctor(Doctor doctor)
    {
        return new AdminDoctorRowResponse(
            doctor.Id,
            doctor.UserId,
            doctor.TenantId,
            doctor.User.FullName,
            doctor.User.Email,
            doctor.User.PhoneNumber,
            doctor.PmdcRegistrationNumber,
            doctor.Specialization,
            doctor.City,
            doctor.ConsultationFee,
            doctor.IsVerified,
            doctor.IsActive,
            doctor.Schedules.Count,
            doctor.CreatedAt);
    }

    private static SystemSettingResponse MapSetting(SystemSetting setting)
    {
        return new SystemSettingResponse(
            setting.Id,
            setting.SettingKey,
            setting.GroupName,
            setting.DisplayName,
            setting.IsSensitive ? "********" : setting.Value,
            setting.ValueType,
            setting.Description,
            setting.IsSensitive,
            setting.IsEditable,
            setting.CreatedAt,
            setting.UpdatedAt);
    }

    private static SystemSetting Setting(
        string settingKey,
        string groupName,
        string displayName,
        string value,
        string valueType,
        string description)
    {
        return new SystemSetting
        {
            SettingKey = settingKey,
            GroupName = groupName,
            DisplayName = displayName,
            Value = value,
            ValueType = valueType,
            Description = description,
            IsEditable = true
        };
    }

    private static SystemSetting CloneSetting(SystemSetting source)
    {
        return new SystemSetting
        {
            SettingKey = source.SettingKey,
            GroupName = source.GroupName,
            DisplayName = source.DisplayName,
            Value = source.Value,
            ValueType = source.ValueType,
            Description = source.Description,
            IsSensitive = source.IsSensitive,
            IsEditable = source.IsEditable
        };
    }

    private sealed record AppointmentStatusCount(AppointmentStatus Status, int Count);
}
