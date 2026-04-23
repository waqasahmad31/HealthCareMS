using HealthCareMS.Application.Admin;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HealthCareMS.Infrastructure.Admin;

public sealed class AdminOperationsService(HealthCareDbContext dbContext, ICurrentUser? currentUser = null) : IAdminOperationsService
{
    private const string NavigationSettingKey = "Platform.Navigation.MenuConfigJson";
    private const string NavigationAssignmentSettingPrefix = "Navigation.Assignment.User.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
        Setting("Performance.SlowQueryThresholdMs", "Performance", "Slow query threshold", "500", "Number", "Threshold used by admins during performance reviews."),
        Setting(
            NavigationSettingKey,
            "Platform",
            "Navigation menu configuration",
            """
            {"groups":[{"key":"general","sortOrder":10,"labels":{"en":"General","ur":"جنرل"},"items":[{"key":"dashboard","label":{"en":"Dashboard","ur":"ڈیش بورڈ"},"icon":"D","route":"","sortOrder":10},{"key":"notifications","label":{"en":"Notifications","ur":"نوٹیفکیشنز"},"icon":"N","route":"notifications","sortOrder":20}]},{"key":"admin","sortOrder":20,"labels":{"en":"Admin","ur":"ایڈمن"},"items":[{"key":"tenants","label":{"en":"Tenants","ur":"ٹیننٹس"},"icon":"T","route":"tenants","sortOrder":10,"requiredPermissions":["system.tenants.create"]},{"key":"doctors","label":{"en":"Doctors","ur":"ڈاکٹرز"},"icon":"V","route":"admin/doctors","sortOrder":20,"requiredPermissions":["doctor.verify"]},{"key":"config","label":{"en":"Configuration","ur":"کنفیگریشن"},"icon":"K","route":"admin/system-configuration","sortOrder":30,"requiredPermissions":["tenant.settings.update"]}]},{"key":"operations","sortOrder":30,"labels":{"en":"Operations","ur":"آپریشنز"},"items":[{"key":"doctor-portal","label":{"en":"Doctor Portal","ur":"ڈاکٹر پورٹل"},"icon":"D","route":"portal/doctor","sortOrder":10,"requiredPermissions":["doctor.schedule.manage"]},{"key":"patient-portal","label":{"en":"Patient Portal","ur":"پیشنٹ پورٹل"},"icon":"U","route":"portal/patient","sortOrder":20,"requiredPermissions":["patient.records.view_own"]},{"key":"pharmacy","label":{"en":"Pharmacy","ur":"فارمیسی"},"icon":"R","route":"pharmacy","sortOrder":30,"requiredPermissions":["pharmacy.medicines.view"]},{"key":"lab","label":{"en":"Laboratory","ur":"لیبارٹری"},"icon":"L","route":"laboratory","sortOrder":40,"requiredPermissions":["lab.tests.view"]}]}]}
            """,
            "Json",
            "JSON configuration for role and permission-based navigation menu (EN/UR).")
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

    public async Task<Result<NavigationMenuResponse>> GetNavigationMenuAsync(
        string? culture,
        CancellationToken cancellationToken)
    {
        if (currentUser?.IsAuthenticated != true)
        {
            return Result<NavigationMenuResponse>.Failure(new Error("NAVIGATION_FORBIDDEN", "Only authenticated users can access navigation."));
        }

        await EnsureDefaultSettingsAsync(cancellationToken);

        var configResult = await GetNavigationConfigPayloadAsync(cancellationToken);
        if (configResult.IsFailure)
        {
            return Result<NavigationMenuResponse>.Failure(configResult.Error);
        }

        var normalizedCulture = string.Equals(culture, "ur", StringComparison.OrdinalIgnoreCase) ? "ur" : "en";
        var assignmentKeys = await GetAssignedMenuKeysAsync(currentUser.UserId!.Value, cancellationToken);
        var groups = configResult.Value.Groups
            .OrderBy(x => x.SortOrder)
            .Select(x => MapGroup(x, normalizedCulture, assignmentKeys))
            .Where(x => x.Items.Count > 0)
            .ToList();

        return Result<NavigationMenuResponse>.Success(new NavigationMenuResponse(normalizedCulture, groups));
    }

    public async Task<Result<NavigationConfigurationResponse>> GetNavigationConfigurationAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultSettingsAsync(cancellationToken);
        var setting = await dbContext.SystemSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingKey == NavigationSettingKey, cancellationToken);
        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return Result<NavigationConfigurationResponse>.Failure(new Error("NAVIGATION_CONFIG_MISSING", "Navigation configuration is missing."));
        }

        return Result<NavigationConfigurationResponse>.Success(new NavigationConfigurationResponse(setting.Value));
    }

    public async Task<Result<NavigationConfigurationResponse>> UpdateNavigationConfigurationAsync(
        UpdateNavigationConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureDefaultSettingsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.ConfigurationJson))
        {
            return Result<NavigationConfigurationResponse>.Failure(Error.Validation([
                new ValidationError(nameof(request.ConfigurationJson), "ConfigurationJson is required.")
            ]));
        }

        NavigationConfigPayload? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<NavigationConfigPayload>(request.ConfigurationJson, JsonOptions);
        }
        catch (JsonException)
        {
            return Result<NavigationConfigurationResponse>.Failure(new Error("NAVIGATION_CONFIG_INVALID", "Navigation configuration JSON is invalid."));
        }

        if (parsed?.Groups is null || parsed.Groups.Count == 0)
        {
            return Result<NavigationConfigurationResponse>.Failure(new Error("NAVIGATION_CONFIG_EMPTY", "Navigation configuration does not contain any groups."));
        }

        var setting = await dbContext.SystemSettings
            .SingleOrDefaultAsync(x => x.SettingKey == NavigationSettingKey, cancellationToken);
        if (setting is null)
        {
            return Result<NavigationConfigurationResponse>.Failure(new Error("NAVIGATION_CONFIG_MISSING", "Navigation configuration is missing."));
        }

        setting.Value = request.ConfigurationJson.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NavigationConfigurationResponse>.Success(new NavigationConfigurationResponse(setting.Value));
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
            "Json" when !IsValidJson(value) => Result.Failure(new Error("ADMIN_SETTING_VALUE_INVALID", "Value must be valid JSON.")),
            _ => Result.Success()
        };
    }

    private static bool IsValidJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private NavigationMenuGroupResponse MapGroup(
        NavigationGroupPayload group,
        string culture,
        IReadOnlySet<string>? assignedKeys)
    {
        var label = SelectLocalizedValue(group.Labels, culture) ?? group.Key;
        var items = (group.Items ?? [])
            .Where(x => IsItemAuthorized(x, assignedKeys))
            .OrderBy(x => x.SortOrder)
            .Select(x => new NavigationMenuItemResponse(
                x.Key,
                SelectLocalizedValue(x.Label, culture) ?? x.Key,
                string.IsNullOrWhiteSpace(x.Icon) ? "?" : x.Icon.Trim(),
                x.Route?.Trim() ?? string.Empty,
                x.SortOrder))
            .ToList();

        return new NavigationMenuGroupResponse(
            group.Key,
            label,
            group.SortOrder,
            items);
    }

    private bool IsItemAuthorized(NavigationItemPayload item, IReadOnlySet<string>? assignedKeys)
    {
        if (currentUser?.IsSuperAdmin == true)
        {
            return true;
        }

        if (assignedKeys is not null && assignedKeys.Count > 0 && !assignedKeys.Contains(item.Key))
        {
            return false;
        }

        if (item.RequiredPermissions is null || item.RequiredPermissions.Count == 0)
        {
            return true;
        }

        return item.RequiredPermissions.Any(permission =>
            currentUser?.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase) == true);
    }

    private async Task<Result<NavigationConfigPayload>> GetNavigationConfigPayloadAsync(CancellationToken cancellationToken)
    {
        var setting = await dbContext.SystemSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingKey == NavigationSettingKey, cancellationToken);
        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return Result<NavigationConfigPayload>.Failure(new Error("NAVIGATION_CONFIG_MISSING", "Navigation configuration is missing."));
        }

        try
        {
            var payload = JsonSerializer.Deserialize<NavigationConfigPayload>(setting.Value, JsonOptions);
            if (payload?.Groups is null || payload.Groups.Count == 0)
            {
                return Result<NavigationConfigPayload>.Failure(new Error("NAVIGATION_CONFIG_EMPTY", "Navigation configuration does not contain any groups."));
            }

            return Result<NavigationConfigPayload>.Success(payload);
        }
        catch (JsonException)
        {
            return Result<NavigationConfigPayload>.Failure(new Error("NAVIGATION_CONFIG_INVALID", "Navigation configuration JSON is invalid."));
        }
    }

    private async Task<IReadOnlySet<string>?> GetAssignedMenuKeysAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settingKey = $"{NavigationAssignmentSettingPrefix}{userId:N}";
        var setting = await dbContext.SystemSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingKey == settingKey, cancellationToken);
        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return null;
        }

        try
        {
            var assignment = JsonSerializer.Deserialize<UserNavigationAssignmentPayload>(setting.Value, JsonOptions);
            return assignment?.MenuItemKeys?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? SelectLocalizedValue(LocalizedTextPayload? localized, string culture)
    {
        if (localized is null)
        {
            return null;
        }

        if (string.Equals(culture, "ur", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(localized.Ur))
        {
            return localized.Ur.Trim();
        }

        return string.IsNullOrWhiteSpace(localized.En) ? null : localized.En.Trim();
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

    private sealed record NavigationConfigPayload(IReadOnlyList<NavigationGroupPayload> Groups);

    private sealed record NavigationGroupPayload(
        string Key,
        int SortOrder,
        LocalizedTextPayload Labels,
        IReadOnlyList<NavigationItemPayload> Items);

    private sealed record NavigationItemPayload(
        string Key,
        LocalizedTextPayload Label,
        string Icon,
        string Route,
        int SortOrder,
        IReadOnlyList<string>? RequiredPermissions);

    private sealed record LocalizedTextPayload(string? En, string? Ur);

    private sealed record UserNavigationAssignmentPayload(IReadOnlyList<string> MenuItemKeys);
}
