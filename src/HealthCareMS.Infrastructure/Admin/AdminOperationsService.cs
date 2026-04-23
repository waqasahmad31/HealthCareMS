using HealthCareMS.Application.Admin;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Configuration;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HealthCareMS.Infrastructure.Admin;

public sealed class AdminOperationsService(HealthCareDbContext dbContext, ICurrentUser? currentUser = null) : IAdminOperationsService
{
    private const string NavigationSettingKey = NavigationDefaults.SettingKey;
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
            NavigationDefaults.ConfigurationJson,
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
        var normalizedCulture = string.Equals(culture, "ur", StringComparison.OrdinalIgnoreCase) ? "ur" : "en";
        var assignmentKeys = await GetAssignedMenuKeysAsync(currentUser.UserId!.Value, cancellationToken);
        var groups = await dbContext.NavigationGroups
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var mapped = groups
            .Select(x => MapGroupFromEntity(x, normalizedCulture, assignmentKeys))
            .Where(x => x.Items.Count > 0)
            .ToList();

        return Result<NavigationMenuResponse>.Success(new NavigationMenuResponse(normalizedCulture, mapped));
    }

    public async Task<Result<NavigationConfigurationResponse>> GetNavigationConfigurationAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultSettingsAsync(cancellationToken);
        var groups = await dbContext.NavigationGroups
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        if (groups.Count == 0)
        {
            return Result<NavigationConfigurationResponse>.Failure(new Error("NAVIGATION_CONFIG_MISSING", "Navigation configuration is missing."));
        }

        var payload = ToNavigationConfigPayload(groups);
        return Result<NavigationConfigurationResponse>.Success(new NavigationConfigurationResponse(JsonSerializer.Serialize(payload, JsonOptions)));
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

        var existingItems = await dbContext.NavigationItems.ToListAsync(cancellationToken);
        dbContext.NavigationItems.RemoveRange(existingItems);
        var existingGroups = await dbContext.NavigationGroups.ToListAsync(cancellationToken);
        dbContext.NavigationGroups.RemoveRange(existingGroups);
        await dbContext.SaveChangesAsync(cancellationToken);

        var groups = new List<NavigationGroup>();
        var items = new List<(NavigationItem Item, string? ParentKey)>();
        foreach (var group in parsed.Groups)
        {
            var entity = new NavigationGroup
            {
                Key = group.Key,
                LabelEn = group.Labels.En ?? group.Key,
                LabelUr = group.Labels.Ur ?? (group.Labels.En ?? group.Key),
                SortOrder = group.SortOrder,
                IsActive = true
            };
            groups.Add(entity);
            dbContext.NavigationGroups.Add(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var group in parsed.Groups)
        {
            var groupEntity = groups.First(x => string.Equals(x.Key, group.Key, StringComparison.OrdinalIgnoreCase));
            foreach (var item in group.Items ?? [])
            {
                CollectItemsForImport(groupEntity, item, null, items);
            }
        }

        foreach (var itemEntry in items)
        {
            dbContext.NavigationItems.Add(itemEntry.Item);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var keyLookup = items.Select(x => x.Item).ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var itemEntry in items.Where(x => !string.IsNullOrWhiteSpace(x.ParentKey)))
        {
            if (!keyLookup.TryGetValue(itemEntry.ParentKey!, out var parent))
            {
                continue;
            }

            itemEntry.Item.ParentItemId = parent.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NavigationConfigurationResponse>.Success(new NavigationConfigurationResponse(request.ConfigurationJson.Trim()));
    }

    public async Task<IReadOnlyList<NavigationGroupResponse>> GetNavigationGroupsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.NavigationGroups
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => new NavigationGroupResponse(x.Id, x.TenantId, x.Key, x.LabelEn, x.LabelUr, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<NavigationGroupResponse>> CreateNavigationGroupAsync(CreateNavigationGroupRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.LabelEn) || string.IsNullOrWhiteSpace(request.LabelUr))
        {
            return Result<NavigationGroupResponse>.Failure(new Error("NAVIGATION_GROUP_INVALID", "Key and labels are required."));
        }

        var normalizedKey = request.Key.Trim();
        var exists = await dbContext.NavigationGroups.AnyAsync(x => x.Key == normalizedKey, cancellationToken);
        if (exists)
        {
            return Result<NavigationGroupResponse>.Failure(new Error("NAVIGATION_GROUP_EXISTS", "Navigation group key already exists."));
        }

        var group = new NavigationGroup
        {
            Key = normalizedKey,
            LabelEn = request.LabelEn.Trim(),
            LabelUr = request.LabelUr.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };
        dbContext.NavigationGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NavigationGroupResponse>.Success(new NavigationGroupResponse(group.Id, group.TenantId, group.Key, group.LabelEn, group.LabelUr, group.SortOrder, group.IsActive));
    }

    public async Task<Result<NavigationGroupResponse>> UpdateNavigationGroupAsync(Guid groupId, UpdateNavigationGroupRequest request, CancellationToken cancellationToken)
    {
        var group = await dbContext.NavigationGroups.SingleOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group is null)
        {
            return Result<NavigationGroupResponse>.Failure(new Error("NAVIGATION_GROUP_NOT_FOUND", "Navigation group was not found."));
        }

        group.LabelEn = request.LabelEn.Trim();
        group.LabelUr = request.LabelUr.Trim();
        group.SortOrder = request.SortOrder;
        group.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NavigationGroupResponse>.Success(new NavigationGroupResponse(group.Id, group.TenantId, group.Key, group.LabelEn, group.LabelUr, group.SortOrder, group.IsActive));
    }

    public async Task<Result> DeleteNavigationGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await dbContext.NavigationGroups
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure(new Error("NAVIGATION_GROUP_NOT_FOUND", "Navigation group was not found."));
        }

        dbContext.NavigationItems.RemoveRange(group.Items);
        dbContext.NavigationGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<NavigationItemResponse>> GetNavigationItemsTreeAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.NavigationItems
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        return BuildNavigationItemTree(items, null);
    }

    public async Task<Result<NavigationItemResponse>> CreateNavigationItemAsync(CreateNavigationItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.LabelEn) || string.IsNullOrWhiteSpace(request.LabelUr))
        {
            return Result<NavigationItemResponse>.Failure(new Error("NAVIGATION_ITEM_INVALID", "Key and labels are required."));
        }

        var groupExists = await dbContext.NavigationGroups.AnyAsync(x => x.Id == request.NavigationGroupId, cancellationToken);
        if (!groupExists)
        {
            return Result<NavigationItemResponse>.Failure(new Error("NAVIGATION_GROUP_NOT_FOUND", "Navigation group was not found."));
        }

        var normalizedKey = request.Key.Trim();
        if (await dbContext.NavigationItems.AnyAsync(x => x.Key == normalizedKey, cancellationToken))
        {
            return Result<NavigationItemResponse>.Failure(new Error("NAVIGATION_ITEM_EXISTS", "Navigation item key already exists."));
        }

        var item = new NavigationItem
        {
            NavigationGroupId = request.NavigationGroupId,
            ParentItemId = request.ParentItemId,
            Key = normalizedKey,
            LabelEn = request.LabelEn.Trim(),
            LabelUr = request.LabelUr.Trim(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "?" : request.Icon.Trim(),
            Route = request.Route?.Trim() ?? string.Empty,
            SortOrder = request.SortOrder,
            RequiredPermissionsJson = JsonSerializer.Serialize((request.RequiredPermissions ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), JsonOptions),
            IsActive = request.IsActive
        };
        dbContext.NavigationItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NavigationItemResponse>.Success(ToNavigationItemResponse(item, []));
    }

    public async Task<Result<NavigationItemResponse>> UpdateNavigationItemAsync(Guid itemId, UpdateNavigationItemRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.NavigationItems.SingleOrDefaultAsync(x => x.Id == itemId, cancellationToken);
        if (item is null)
        {
            return Result<NavigationItemResponse>.Failure(new Error("NAVIGATION_ITEM_NOT_FOUND", "Navigation item was not found."));
        }

        item.NavigationGroupId = request.NavigationGroupId;
        item.ParentItemId = request.ParentItemId;
        item.LabelEn = request.LabelEn.Trim();
        item.LabelUr = request.LabelUr.Trim();
        item.Icon = string.IsNullOrWhiteSpace(request.Icon) ? "?" : request.Icon.Trim();
        item.Route = request.Route?.Trim() ?? string.Empty;
        item.SortOrder = request.SortOrder;
        item.RequiredPermissionsJson = JsonSerializer.Serialize((request.RequiredPermissions ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), JsonOptions);
        item.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NavigationItemResponse>.Success(ToNavigationItemResponse(item, []));
    }

    public async Task<Result> DeleteNavigationItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var items = await dbContext.NavigationItems.ToListAsync(cancellationToken);
        var target = items.SingleOrDefault(x => x.Id == itemId);
        if (target is null)
        {
            return Result.Failure(new Error("NAVIGATION_ITEM_NOT_FOUND", "Navigation item was not found."));
        }

        var descendantIds = GetDescendantIds(items, itemId);
        descendantIds.Add(itemId);
        var toDelete = items.Where(x => descendantIds.Contains(x.Id)).ToList();
        dbContext.NavigationItems.RemoveRange(toDelete);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<NavigationIconResponse>> GetNavigationIconsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.NavigationIcons
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new NavigationIconResponse(x.Id, x.Key, x.Symbol, x.Description, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<NavigationIconResponse>> CreateNavigationIconAsync(CreateNavigationIconRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Symbol))
        {
            return Result<NavigationIconResponse>.Failure(new Error("NAVIGATION_ICON_INVALID", "Key and symbol are required."));
        }

        var key = request.Key.Trim();
        if (await dbContext.NavigationIcons.AnyAsync(x => x.Key == key, cancellationToken))
        {
            return Result<NavigationIconResponse>.Failure(new Error("NAVIGATION_ICON_EXISTS", "Navigation icon key already exists."));
        }

        var icon = new NavigationIcon
        {
            Key = key,
            Symbol = request.Symbol.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };
        dbContext.NavigationIcons.Add(icon);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NavigationIconResponse>.Success(new NavigationIconResponse(icon.Id, icon.Key, icon.Symbol, icon.Description, icon.IsActive));
    }

    public async Task<Result<NavigationIconResponse>> UpdateNavigationIconAsync(Guid iconId, UpdateNavigationIconRequest request, CancellationToken cancellationToken)
    {
        var icon = await dbContext.NavigationIcons.SingleOrDefaultAsync(x => x.Id == iconId, cancellationToken);
        if (icon is null)
        {
            return Result<NavigationIconResponse>.Failure(new Error("NAVIGATION_ICON_NOT_FOUND", "Navigation icon was not found."));
        }

        icon.Symbol = request.Symbol.Trim();
        icon.Description = request.Description?.Trim();
        icon.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NavigationIconResponse>.Success(new NavigationIconResponse(icon.Id, icon.Key, icon.Symbol, icon.Description, icon.IsActive));
    }

    public async Task<Result> DeleteNavigationIconAsync(Guid iconId, CancellationToken cancellationToken)
    {
        var icon = await dbContext.NavigationIcons.SingleOrDefaultAsync(x => x.Id == iconId, cancellationToken);
        if (icon is null)
        {
            return Result.Failure(new Error("NAVIGATION_ICON_NOT_FOUND", "Navigation icon was not found."));
        }

        dbContext.NavigationIcons.Remove(icon);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
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

    private NavigationMenuGroupResponse MapGroupFromEntity(
        NavigationGroup group,
        string culture,
        IReadOnlySet<string>? assignedKeys)
    {
        var items = BuildMenuTree(group.Items, null, culture, assignedKeys);
        return new NavigationMenuGroupResponse(
            group.Key,
            SelectLocalizedValue(group.LabelEn, group.LabelUr, culture),
            group.SortOrder,
            items);
    }

    private List<NavigationMenuItemResponse> BuildMenuTree(
        IEnumerable<NavigationItem> allItems,
        Guid? parentId,
        string culture,
        IReadOnlySet<string>? assignedKeys)
    {
        return allItems
            .Where(x => x.ParentItemId == parentId && x.IsActive)
            .Where(x => IsItemAuthorized(x, assignedKeys))
            .OrderBy(x => x.SortOrder)
            .Select(x => new NavigationMenuItemResponse(
                x.Key,
                SelectLocalizedValue(x.LabelEn, x.LabelUr, culture),
                string.IsNullOrWhiteSpace(x.Icon) ? "?" : x.Icon.Trim(),
                x.Route?.Trim() ?? string.Empty,
                x.SortOrder,
                BuildMenuTree(allItems, x.Id, culture, assignedKeys)))
            .ToList();
    }

    private bool IsItemAuthorized(NavigationItem item, IReadOnlySet<string>? assignedKeys)
    {
        if (currentUser?.IsSuperAdmin == true)
        {
            return true;
        }

        if (assignedKeys is not null && assignedKeys.Count > 0 && !assignedKeys.Contains(item.Key))
        {
            return false;
        }

        var requiredPermissions = ReadRequiredPermissions(item.RequiredPermissionsJson);
        if (requiredPermissions.Count == 0)
        {
            return true;
        }

        return requiredPermissions.Any(permission =>
            currentUser?.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase) == true);
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

    private static string SelectLocalizedValue(string en, string ur, string culture)
    {
        if (string.Equals(culture, "ur", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(ur))
        {
            return ur.Trim();
        }

        return string.IsNullOrWhiteSpace(en) ? ur.Trim() : en.Trim();
    }

    private static IReadOnlyList<string> ReadRequiredPermissions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var permissions = JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
            return permissions
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static NavigationConfigPayload ToNavigationConfigPayload(IReadOnlyList<NavigationGroup> groups)
    {
        return new NavigationConfigPayload(
            groups
                .OrderBy(x => x.SortOrder)
                .Select(group =>
                    new NavigationGroupPayload(
                        group.Key,
                        group.SortOrder,
                        new LocalizedTextPayload(group.LabelEn, group.LabelUr),
                        BuildGroupItemsForPayload(group.Items, null)))
                .ToArray());
    }

    private static IReadOnlyList<NavigationItemPayload> BuildGroupItemsForPayload(ICollection<NavigationItem> items, Guid? parentItemId)
    {
        return items
            .Where(x => x.ParentItemId == parentItemId)
            .OrderBy(x => x.SortOrder)
            .Select(item => new NavigationItemPayload(
                item.Key,
                new LocalizedTextPayload(item.LabelEn, item.LabelUr),
                item.Icon,
                item.Route,
                item.SortOrder,
                ReadRequiredPermissions(item.RequiredPermissionsJson),
                BuildGroupItemsForPayload(items, item.Id)))
            .ToArray();
    }

    private static void CollectItemsForImport(
        NavigationGroup group,
        NavigationItemPayload payload,
        string? parentKey,
        ICollection<(NavigationItem Item, string? ParentKey)> items)
    {
        var entity = new NavigationItem
        {
            NavigationGroupId = group.Id,
            Key = payload.Key,
            LabelEn = payload.Label.En ?? payload.Key,
            LabelUr = payload.Label.Ur ?? (payload.Label.En ?? payload.Key),
            Icon = string.IsNullOrWhiteSpace(payload.Icon) ? "?" : payload.Icon.Trim(),
            Route = payload.Route?.Trim() ?? string.Empty,
            SortOrder = payload.SortOrder,
            RequiredPermissionsJson = JsonSerializer.Serialize((payload.RequiredPermissions ?? []).ToArray(), JsonOptions),
            IsActive = true
        };

        items.Add((entity, parentKey));
        foreach (var child in payload.Children ?? [])
        {
            CollectItemsForImport(group, child, payload.Key, items);
        }
    }

    private static IReadOnlyList<NavigationItemResponse> BuildNavigationItemTree(IReadOnlyList<NavigationItem> items, Guid? parentId)
    {
        return items
            .Where(x => x.ParentItemId == parentId)
            .OrderBy(x => x.SortOrder)
            .Select(x => ToNavigationItemResponse(x, BuildNavigationItemTree(items, x.Id)))
            .ToArray();
    }

    private static NavigationItemResponse ToNavigationItemResponse(NavigationItem item, IReadOnlyList<NavigationItemResponse> children)
    {
        return new NavigationItemResponse(
            item.Id,
            item.NavigationGroupId,
            item.ParentItemId,
            item.Key,
            item.LabelEn,
            item.LabelUr,
            item.Icon,
            item.Route,
            item.SortOrder,
            ReadRequiredPermissions(item.RequiredPermissionsJson),
            item.IsActive,
            children);
    }

    private static HashSet<Guid> GetDescendantIds(IReadOnlyList<NavigationItem> items, Guid parentId)
    {
        var children = items.Where(x => x.ParentItemId == parentId).Select(x => x.Id).ToList();
        var result = new HashSet<Guid>(children);
        foreach (var child in children)
        {
            result.UnionWith(GetDescendantIds(items, child));
        }

        return result;
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
        IReadOnlyList<string>? RequiredPermissions,
        IReadOnlyList<NavigationItemPayload>? Children);

    private sealed record LocalizedTextPayload(string? En, string? Ur);

    private sealed record UserNavigationAssignmentPayload(IReadOnlyList<string> MenuItemKeys);
}
