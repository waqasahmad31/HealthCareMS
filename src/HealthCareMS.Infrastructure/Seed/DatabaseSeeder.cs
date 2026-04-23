using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Labs;
using HealthCareMS.Domain.Notifications;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Domain.Pharmacy;
using HealthCareMS.Infrastructure.Configuration;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HealthCareMS.Infrastructure.Seed;

public sealed class DatabaseSeeder(
    HealthCareDbContext dbContext,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    private const string NavigationSettingKey = NavigationDefaults.SettingKey;

    public async Task SeedAsync(bool seedDemoData, CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedSuperAdminAsync(cancellationToken);
        await SeedSystemSettingsAsync(cancellationToken);
        await SeedNavigationEntitiesAsync(cancellationToken);
        if (seedDemoData)
        {
            await SeedAllDomainTablesAsync(cancellationToken);
        }
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existingKeys = await dbContext.Permissions
            .Select(x => x.PermissionKey)
            .ToListAsync(cancellationToken);

        var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var permissionKey in PermissionKeys.All.Where(x => !existing.Contains(x)))
        {
            dbContext.Permissions.Add(new Permission
            {
                PermissionKey = permissionKey,
                Module = ToModule(permissionKey),
                Action = ToAction(permissionKey),
                Description = permissionKey
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var superAdminRole = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.TenantId == null && x.Name == "SuperAdmin", cancellationToken);

        if (superAdminRole is null)
        {
            superAdminRole = new Role { Name = "SuperAdmin", IsSystemRole = true };
            dbContext.Roles.Add(superAdminRole);
        }

        var platformRoleNames = new[] { "Patient", "Doctor", "LabAdmin", "LabStaff", "PharmacyAdmin", "Pharmacist", "DeliveryAgent" };
        foreach (var roleName in platformRoleNames)
        {
            var exists = await dbContext.Roles.AnyAsync(x => x.TenantId == null && x.Name == roleName, cancellationToken);
            if (!exists)
            {
                dbContext.Roles.Add(new Role { Name = roleName, IsSystemRole = true });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var allPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        var currentPermissionIds = superAdminRole.RolePermissions.Select(x => x.PermissionId).ToHashSet();
        foreach (var permission in allPermissions.Where(x => !currentPermissionIds.Contains(x.Id)))
        {
            superAdminRole.RolePermissions.Add(new RolePermission
            {
                RoleId = superAdminRole.Id,
                PermissionId = permission.Id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureRolePermissionsAsync(
            "LabAdmin",
            PermissionKeys.Lab.All,
            allPermissions,
            cancellationToken);
        await EnsureRolePermissionsAsync(
            "LabStaff",
            [
                PermissionKeys.Lab.TestsView,
                PermissionKeys.Lab.BookingCreate,
                PermissionKeys.Lab.SampleCollect,
                PermissionKeys.Lab.ResultsEntry,
                PermissionKeys.Lab.ResultsValidate,
                PermissionKeys.Lab.ResultsRelease,
                PermissionKeys.Lab.ReportsDownload
            ],
            allPermissions,
            cancellationToken);
        await EnsureRolePermissionsAsync(
            "PharmacyAdmin",
            PermissionKeys.Pharmacy.All,
            allPermissions,
            cancellationToken);
        await EnsureRolePermissionsAsync(
            "Pharmacist",
            [
                PermissionKeys.Pharmacy.MedicinesView,
                PermissionKeys.Pharmacy.StockView,
                PermissionKeys.Pharmacy.OrdersView,
                PermissionKeys.Pharmacy.OrdersProcess,
                PermissionKeys.Pharmacy.Dispense
            ],
            allPermissions,
            cancellationToken);
        await EnsureRolePermissionsAsync(
            "DeliveryAgent",
            [
                PermissionKeys.Pharmacy.OrdersView,
                PermissionKeys.Pharmacy.OrdersProcess
            ],
            allPermissions,
            cancellationToken);
    }

    private async Task EnsureRolePermissionsAsync(
        string roleName,
        IReadOnlyCollection<string> permissionKeys,
        IReadOnlyList<Permission> allPermissions,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .SingleAsync(x => x.TenantId == null && x.Name == roleName, cancellationToken);
        var permissionIds = allPermissions
            .Where(x => permissionKeys.Contains(x.PermissionKey, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .ToHashSet();
        var existing = role.RolePermissions.Select(x => x.PermissionId).ToHashSet();

        foreach (var permissionId in permissionIds.Where(x => !existing.Contains(x)))
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
    {
        var email = configuration["SuperAdmin:Email"]?.Trim().ToLowerInvariant() ?? "superadmin@healthcarems.local";
        var password = configuration["SuperAdmin:Password"] ?? "ChangeMe@12345";

        var exists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            return;
        }

        var role = await dbContext.Roles.SingleAsync(x => x.TenantId == null && x.Name == "SuperAdmin", cancellationToken);

        dbContext.Users.Add(new ApplicationUser
        {
            RoleId = role.Id,
            FirstName = "Super",
            LastName = "Admin",
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            IsActive = true,
            IsEmailVerified = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded Super Admin account {Email}.", email);
    }

    private async Task SeedSystemSettingsAsync(CancellationToken cancellationToken)
    {
        var defaults = new[]
        {
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
        };

        var existingKeys = await dbContext.SystemSettings
            .Select(x => x.SettingKey)
            .ToListAsync(cancellationToken);
        var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in defaults.Where(x => !existing.Contains(x.SettingKey)))
        {
            dbContext.SystemSettings.Add(setting);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedNavigationEntitiesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.NavigationGroups.AnyAsync(cancellationToken))
        {
            return;
        }

        var settingsJson = await dbContext.SystemSettings
            .Where(x => x.SettingKey == NavigationSettingKey)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);

        var json = string.IsNullOrWhiteSpace(settingsJson) ? NavigationDefaults.ConfigurationJson : settingsJson;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            document = JsonDocument.Parse(NavigationDefaults.ConfigurationJson);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("groups", out var groupsElement) || groupsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var groupByKey = new Dictionary<string, NavigationGroup>(StringComparer.OrdinalIgnoreCase);
            var itemsToAdd = new List<(NavigationItem Item, string? ParentKey)>();
            foreach (var groupElement in groupsElement.EnumerateArray())
            {
                var groupKey = groupElement.GetProperty("key").GetString()?.Trim() ?? Guid.NewGuid().ToString("N");
                var sortOrder = groupElement.TryGetProperty("sortOrder", out var groupSortElement) && groupSortElement.TryGetInt32(out var gs) ? gs : 0;

                var labelEn = groupKey;
                var labelUr = groupKey;
                if (groupElement.TryGetProperty("labels", out var labelsElement) && labelsElement.ValueKind == JsonValueKind.Object)
                {
                    labelEn = labelsElement.TryGetProperty("en", out var enElement) ? enElement.GetString() ?? groupKey : groupKey;
                    labelUr = labelsElement.TryGetProperty("ur", out var urElement) ? urElement.GetString() ?? labelEn : labelEn;
                }

                var group = new NavigationGroup
                {
                    Key = groupKey,
                    LabelEn = labelEn,
                    LabelUr = labelUr,
                    SortOrder = sortOrder,
                    IsActive = true
                };
                dbContext.NavigationGroups.Add(group);
                groupByKey[groupKey] = group;

                if (!groupElement.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var itemElement in itemsElement.EnumerateArray())
                {
                    ParseNavigationItemRecursive(group, itemElement, null, itemsToAdd);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var itemKeyLookup = new Dictionary<string, NavigationItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var itemEntry in itemsToAdd)
            {
                dbContext.NavigationItems.Add(itemEntry.Item);
                itemKeyLookup[itemEntry.Item.Key] = itemEntry.Item;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var itemEntry in itemsToAdd.Where(x => !string.IsNullOrWhiteSpace(x.ParentKey)))
            {
                if (!itemKeyLookup.TryGetValue(itemEntry.ParentKey!, out var parent))
                {
                    continue;
                }

                itemEntry.Item.ParentItemId = parent.Id;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.NavigationIcons.AnyAsync(cancellationToken))
        {
            dbContext.NavigationIcons.AddRange(
                new NavigationIcon { Key = "dashboard", LabelEn = "Dashboard", LabelUr = "ڈیش بورڈ", CssClass = "bi bi-grid-1x2-fill", Symbol = "D", Description = "Dashboard" },
                new NavigationIcon { Key = "notifications", LabelEn = "Notifications", LabelUr = "نوٹیفیکیشنز", CssClass = "bi bi-bell-fill", Symbol = "N", Description = "Notifications" },
                new NavigationIcon { Key = "tenants", LabelEn = "Tenants", LabelUr = "ٹیننٹس", CssClass = "bi bi-buildings-fill", Symbol = "T", Description = "Tenants" },
                new NavigationIcon { Key = "doctors", LabelEn = "Doctors", LabelUr = "ڈاکٹرز", CssClass = "bi bi-person-badge-fill", Symbol = "V", Description = "Doctors" },
                new NavigationIcon { Key = "config", LabelEn = "Configuration", LabelUr = "کنفیگریشن", CssClass = "bi bi-sliders2", Symbol = "K", Description = "Configuration" },
                new NavigationIcon { Key = "doctor-portal", LabelEn = "Doctor Portal", LabelUr = "ڈاکٹر پورٹل", CssClass = "bi bi-heart-pulse-fill", Symbol = "P", Description = "Doctor Portal" },
                new NavigationIcon { Key = "patient-portal", LabelEn = "Patient Portal", LabelUr = "پیشنٹ پورٹل", CssClass = "bi bi-person-lines-fill", Symbol = "U", Description = "Patient Portal" },
                new NavigationIcon { Key = "pharmacy", LabelEn = "Pharmacy", LabelUr = "فارمیسی", CssClass = "bi bi-capsule-pill", Symbol = "R", Description = "Pharmacy" },
                new NavigationIcon { Key = "lab", LabelEn = "Laboratory", LabelUr = "لیبارٹری", CssClass = "bi bi-clipboard2-pulse-fill", Symbol = "L", Description = "Laboratory" });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ParseNavigationItemRecursive(
        NavigationGroup group,
        JsonElement itemElement,
        string? parentKey,
        ICollection<(NavigationItem Item, string? ParentKey)> output)
    {
        if (!itemElement.TryGetProperty("key", out var keyElement) || keyElement.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var key = keyElement.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var labelEn = key;
        var labelUr = key;
        if (itemElement.TryGetProperty("label", out var labelElement) && labelElement.ValueKind == JsonValueKind.Object)
        {
            labelEn = labelElement.TryGetProperty("en", out var enElement) ? enElement.GetString() ?? key : key;
            labelUr = labelElement.TryGetProperty("ur", out var urElement) ? urElement.GetString() ?? labelEn : labelEn;
        }

        var icon = itemElement.TryGetProperty("icon", out var iconElement) ? (iconElement.GetString() ?? "?") : "?";
        var route = itemElement.TryGetProperty("route", out var routeElement) ? (routeElement.GetString() ?? string.Empty) : string.Empty;
        var sortOrder = itemElement.TryGetProperty("sortOrder", out var sortElement) && sortElement.TryGetInt32(out var so) ? so : 0;

        var requiredPermissions = "[]";
        if (itemElement.TryGetProperty("requiredPermissions", out var permissionsElement))
        {
            requiredPermissions = permissionsElement.GetRawText();
        }

        var item = new NavigationItem
        {
            NavigationGroupId = group.Id,
            Key = key,
            LabelEn = labelEn,
            LabelUr = labelUr,
            Icon = icon,
            Route = route,
            SortOrder = sortOrder,
            RequiredPermissionsJson = requiredPermissions,
            ParentItemId = null,
            IsActive = true
        };

        output.Add((item, parentKey));

        if (!itemElement.TryGetProperty("children", out var childrenElement) || childrenElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var childElement in childrenElement.EnumerateArray())
        {
            ParseNavigationItemRecursive(group, childElement, key, output);
        }
    }

    private async Task SeedAllDomainTablesAsync(CancellationToken cancellationToken)
    {
        var superAdminEmail = configuration["SuperAdmin:Email"]?.Trim().ToLowerInvariant() ?? "superadmin@healthcarems.local";
        var superAdmin = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == superAdminEmail, cancellationToken);
        if (superAdmin is null)
        {
            return;
        }

        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Name = "Seeded Care Hospital",
                TenantType = TenantType.Hospital,
                OwnerName = "Seed Owner",
                Phone = "03000000000",
                Email = "tenant.seed@healthcarems.local",
                Address = "{\"city\":\"Karachi\"}",
                SubscriptionPlan = SubscriptionPlan.Premium,
                MaxUsers = 100,
                EnabledModules = "{\"doctor\":true,\"patient\":true,\"lab\":true,\"pharmacy\":true}",
                PaymentGateways = "{\"cash\":true}",
                CreatedBySuperAdminId = superAdmin.Id
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var doctorRoleId = await dbContext.Roles.Where(x => x.TenantId == null && x.Name == "Doctor").Select(x => x.Id).SingleAsync(cancellationToken);
        var patientRoleId = await dbContext.Roles.Where(x => x.TenantId == null && x.Name == "Patient").Select(x => x.Id).SingleAsync(cancellationToken);
        var labAdminRoleId = await dbContext.Roles.Where(x => x.TenantId == null && x.Name == "LabAdmin").Select(x => x.Id).SingleAsync(cancellationToken);
        var pharmacistRoleId = await dbContext.Roles.Where(x => x.TenantId == null && x.Name == "Pharmacist").Select(x => x.Id).SingleAsync(cancellationToken);
        var deliveryAgentRoleId = await dbContext.Roles.Where(x => x.TenantId == null && x.Name == "DeliveryAgent").Select(x => x.Id).SingleAsync(cancellationToken);

        var doctorUser = await EnsureUserAsync("doctor.seed@healthcarems.local", "Demo", "Doctor", doctorRoleId, tenant.Id, cancellationToken);
        var patientUser = await EnsureUserAsync("patient.seed@healthcarems.local", "Demo", "Patient", patientRoleId, tenant.Id, cancellationToken);
        var labAdminUser = await EnsureUserAsync("lab.seed@healthcarems.local", "Demo", "LabAdmin", labAdminRoleId, tenant.Id, cancellationToken);
        _ = await EnsureUserAsync("pharmacist.seed@healthcarems.local", "Demo", "Pharmacist", pharmacistRoleId, tenant.Id, cancellationToken);
        var deliveryAgentUser = await EnsureUserAsync("delivery.seed@healthcarems.local", "Demo", "Delivery", deliveryAgentRoleId, tenant.Id, cancellationToken);

        if (!await dbContext.UserPermissions.AnyAsync(cancellationToken))
        {
            var permissionId = await dbContext.Permissions
                .Where(x => x.PermissionKey == PermissionKeys.Lab.TestsView)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (permissionId != Guid.Empty)
            {
                dbContext.UserPermissions.Add(new UserPermission
                {
                    UserId = labAdminUser.Id,
                    PermissionId = permissionId,
                    IsGranted = true,
                    GrantedByUserId = superAdmin.Id
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        var patient = await dbContext.Patients.FirstOrDefaultAsync(cancellationToken);
        if (patient is null)
        {
            patient = new Patient
            {
                UserId = patientUser.Id,
                FirstName = "Demo",
                LastName = "Patient",
                DateOfBirth = new DateOnly(1992, 1, 10),
                Gender = Gender.Male,
                BloodGroup = "B+",
                Phone = "03110000000",
                AddressCity = "Karachi"
            };
            dbContext.Patients.Add(patient);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.MedicalHistories.AnyAsync(cancellationToken))
        {
            dbContext.MedicalHistories.Add(new MedicalHistory
            {
                PatientId = patient.Id,
                Allergies = "[\"Dust\"]",
                ChronicDiseases = "[\"Hypertension\"]"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.PatientVitals.AnyAsync(cancellationToken))
        {
            dbContext.PatientVitals.Add(new PatientVital
            {
                PatientId = patient.Id,
                SystolicBloodPressure = 120,
                DiastolicBloodPressure = 80,
                HeartRate = 74,
                BloodSugarMgDl = 102,
                BloodSugarContext = "Random",
                WeightKg = 72.5m,
                TemperatureCelsius = 36.8m
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var doctor = await dbContext.Doctors.FirstOrDefaultAsync(cancellationToken);
        if (doctor is null)
        {
            doctor = new Doctor
            {
                UserId = doctorUser.Id,
                TenantId = tenant.Id,
                PmdcRegistrationNumber = "PMDC-SEED-001",
                Specialization = "General Physician",
                City = "Karachi",
                ConsultationFee = 2500,
                IsVerified = true,
                IsActive = true
            };
            dbContext.Doctors.Add(doctor);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.DoctorSchedules.AnyAsync(cancellationToken))
        {
            dbContext.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctor.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(14, 0),
                SlotDurationMinutes = 30
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.DrapMedicines.AnyAsync(cancellationToken))
        {
            dbContext.DrapMedicines.Add(new DrapMedicine
            {
                DrapRegistrationNumber = "DRAP-SEED-001",
                BrandName = "Amoxil Seed",
                GenericName = "Amoxicillin",
                DosageForm = "Capsule",
                Strength = "500mg",
                Manufacturer = "Seed Pharma",
                AllergenKeywords = "penicillin"
            });
        }

        var appointment = await dbContext.Appointments.FirstOrDefaultAsync(cancellationToken);
        if (appointment is null)
        {
            var scheduledAt = DateTimeOffset.UtcNow.AddHours(4);
            appointment = new Appointment
            {
                AppointmentNumber = $"APT-SEED-{DateTime.UtcNow:yyyyMMddHHmm}",
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ScheduledAt = scheduledAt,
                EndAt = scheduledAt.AddMinutes(30),
                DurationMinutes = 30,
                Type = AppointmentType.Online,
                Status = AppointmentStatus.Confirmed,
                Priority = AppointmentPriority.Normal,
                ReasonForVisit = "Seeded checkup",
                ConsultationFee = doctor.ConsultationFee,
                PaymentStatus = PaymentStatus.Paid,
                MeetingLink = BuildSeedMeetingLink()
            };
            dbContext.Appointments.Add(appointment);
        }

        var prescription = await dbContext.Prescriptions.FirstOrDefaultAsync(cancellationToken);
        if (prescription is null && appointment is not null)
        {
            prescription = new Prescription
            {
                PrescriptionNumber = $"RX-SEED-{DateTime.UtcNow:yyyyMMddHHmm}",
                AppointmentId = appointment.Id,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = "Seeded diagnosis",
                VerificationCode = "SEED123",
                DigitalSignature = "seed-signature",
                ValidUntil = DateTimeOffset.UtcNow.AddDays(30)
            };
            dbContext.Prescriptions.Add(prescription);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (prescription is not null && !await dbContext.PrescriptionItems.AnyAsync(cancellationToken))
        {
            dbContext.PrescriptionItems.Add(new PrescriptionItem
            {
                PrescriptionId = prescription.Id,
                SortOrder = 1,
                MedicineName = "Amoxicillin",
                Dosage = "1 capsule",
                Frequency = "BID",
                DurationDays = 5,
                Quantity = 10
            });
        }

        if (!await dbContext.ConsultationSessions.AnyAsync(cancellationToken) && appointment is not null)
        {
            dbContext.ConsultationSessions.Add(new ConsultationSession
            {
                AppointmentId = appointment.Id,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ChannelName = "seed-consultation-channel",
                MeetingLink = appointment.MeetingLink ?? BuildSeedMeetingLink(),
                LastTokenIssuedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var session = await dbContext.ConsultationSessions.FirstOrDefaultAsync(cancellationToken);
        if (session is not null && !await dbContext.ChatMessages.AnyAsync(cancellationToken))
        {
            dbContext.ChatMessages.Add(new ChatMessage
            {
                SessionId = session.Id,
                SenderUserId = doctorUser.Id,
                SenderType = "Doctor",
                SenderDisplayName = doctorUser.FullName,
                MessageType = ChatMessageType.Text,
                MessageText = "Welcome to seeded consultation."
            });
        }

        if (!await dbContext.NotificationPreferences.AnyAsync(cancellationToken))
        {
            dbContext.NotificationPreferences.Add(new NotificationPreference
            {
                UserId = patientUser.Id,
                EmailEnabled = true,
                SmsEnabled = true,
                InAppEnabled = true
            });
        }

        if (!await dbContext.Notifications.AnyAsync(cancellationToken))
        {
            dbContext.Notifications.Add(new Notification
            {
                RecipientUserId = patientUser.Id,
                TenantId = tenant.Id,
                Channel = NotificationChannel.InApp,
                Type = NotificationType.AppointmentBooked,
                Status = NotificationStatus.Sent,
                Subject = "Appointment Confirmed",
                Body = "Your seeded appointment has been confirmed.",
                SentAt = DateTimeOffset.UtcNow
            });
        }

        if (!await dbContext.LabTests.AnyAsync(cancellationToken))
        {
            dbContext.LabTests.AddRange(
                new LabTest
                {
                    TenantId = tenant.Id,
                    TestCode = "CBC-SEED",
                    TestName = "Complete Blood Count",
                    Category = "Hematology",
                    Price = 1200
                },
                new LabTest
                {
                    TenantId = tenant.Id,
                    TestCode = "BSR-SEED",
                    TestName = "Blood Sugar Random",
                    Category = "Chemistry",
                    Price = 600
                });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var firstLabTest = await dbContext.LabTests.OrderBy(x => x.TestCode).FirstAsync(cancellationToken);
        var labPanel = await dbContext.LabPanels.FirstOrDefaultAsync(cancellationToken);
        if (labPanel is null)
        {
            labPanel = new LabPanel
            {
                TenantId = tenant.Id,
                PanelCode = "BASIC-SEED",
                PanelName = "Basic Health Panel",
                Category = "General",
                Price = 1600
            };
            dbContext.LabPanels.Add(labPanel);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.LabPanelItems.AnyAsync(cancellationToken))
        {
            dbContext.LabPanelItems.Add(new LabPanelItem
            {
                LabPanelId = labPanel.Id,
                LabTestId = firstLabTest.Id
            });
        }

        var labBooking = await dbContext.LabSampleBookings.FirstOrDefaultAsync(cancellationToken);
        if (labBooking is null)
        {
            labBooking = new LabSampleBooking
            {
                BookingNumber = $"LAB-SEED-{DateTime.UtcNow:yyyyMMddHHmm}",
                TenantId = tenant.Id,
                PatientId = patient.Id,
                AppointmentId = appointment?.Id,
                PrescriptionId = prescription?.Id,
                CollectionType = LabCollectionType.OnSite,
                Status = LabBookingStatus.Ordered,
                SampleBarcode = "SMP-SEED-001",
                TokenNumber = "LQ-SEED-001",
                SubTotal = firstLabTest.Price,
                TotalAmount = firstLabTest.Price
            };
            dbContext.LabSampleBookings.Add(labBooking);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.LabBookingItems.AnyAsync(cancellationToken))
        {
            dbContext.LabBookingItems.Add(new LabBookingItem
            {
                BookingId = labBooking.Id,
                LabTestId = firstLabTest.Id,
                Price = firstLabTest.Price
            });
        }

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(cancellationToken);
        if (supplier is null)
        {
            supplier = new Supplier
            {
                TenantId = tenant.Id,
                Name = "Seed Medical Supplier",
                ContactPerson = "Supplier Rep",
                Phone = "03220000000",
                Email = "supplier.seed@healthcarems.local",
                Address = "Karachi"
            };
            dbContext.Suppliers.Add(supplier);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var medicine = await dbContext.Medicines.FirstOrDefaultAsync(cancellationToken);
        if (medicine is null)
        {
            medicine = new Medicine
            {
                TenantId = tenant.Id,
                GenericName = "Paracetamol",
                BrandName = "Panadol Seed",
                DosageForm = "Tablet",
                Strength = "500mg",
                DrapRegistrationNumber = "DRAP-PHARM-SEED-01",
                Manufacturer = "Seed Pharma",
                UnitPrice = 25,
                UnitCostPrice = 18,
                ReorderLevel = 20,
                Barcode = "BC-SEED-001"
            };
            dbContext.Medicines.Add(medicine);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var stockBatch = await dbContext.StockBatches.FirstOrDefaultAsync(cancellationToken);
        if (stockBatch is null)
        {
            stockBatch = new StockBatch
            {
                TenantId = tenant.Id,
                MedicineId = medicine.Id,
                SupplierId = supplier.Id,
                BatchNumber = "BATCH-SEED-001",
                ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(9)),
                QuantityOnHand = 100,
                UnitCostPrice = medicine.UnitCostPrice
            };
            dbContext.StockBatches.Add(stockBatch);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.StockAdjustments.AnyAsync(cancellationToken))
        {
            dbContext.StockAdjustments.Add(new StockAdjustment
            {
                TenantId = tenant.Id,
                MedicineId = medicine.Id,
                StockBatchId = stockBatch.Id,
                AdjustmentType = StockAdjustmentType.Increase,
                QuantityDelta = 100,
                PreviousQuantity = 0,
                NewQuantity = 100,
                Reason = "Initial seeded stock"
            });
        }

        if (!await dbContext.StockAlerts.AnyAsync(cancellationToken))
        {
            dbContext.StockAlerts.Add(new StockAlert
            {
                TenantId = tenant.Id,
                MedicineId = medicine.Id,
                StockBatchId = stockBatch.Id,
                AlertType = StockAlertType.LowStock,
                Severity = "Medium",
                Message = "Seed alert sample",
                ThresholdQuantity = medicine.ReorderLevel,
                QuantityOnHand = stockBatch.QuantityOnHand
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var prescriptionItem = await dbContext.PrescriptionItems.FirstOrDefaultAsync(cancellationToken);
        var dispense = await dbContext.PrescriptionDispenses.FirstOrDefaultAsync(cancellationToken);
        if (dispense is null && prescription is not null)
        {
            dispense = new PrescriptionDispense
            {
                TenantId = tenant.Id,
                DispenseNumber = $"DSP-SEED-{DateTime.UtcNow:yyyyMMddHHmm}",
                ReceiptNumber = $"RCPT-SEED-{DateTime.UtcNow:yyyyMMddHHmm}",
                PrescriptionId = prescription.Id,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                VerificationCode = prescription.VerificationCode,
                SubTotal = medicine.UnitPrice * 2,
                TotalAmount = medicine.UnitPrice * 2,
                Notes = "Seeded dispense"
            };
            dbContext.PrescriptionDispenses.Add(dispense);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var dispenseItem = await dbContext.PrescriptionDispenseItems.FirstOrDefaultAsync(cancellationToken);
        if (dispenseItem is null && dispense is not null && prescriptionItem is not null)
        {
            dispenseItem = new PrescriptionDispenseItem
            {
                PrescriptionDispenseId = dispense.Id,
                PrescriptionItemId = prescriptionItem.Id,
                MedicineId = medicine.Id,
                PrescribedMedicineName = prescriptionItem.MedicineName,
                DispensedMedicineName = medicine.BrandName,
                QuantityPrescribed = prescriptionItem.Quantity,
                QuantityDispensed = 2,
                UnitPrice = medicine.UnitPrice,
                LineTotal = medicine.UnitPrice * 2
            };
            dbContext.PrescriptionDispenseItems.Add(dispenseItem);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.PrescriptionDispenseBatches.AnyAsync(cancellationToken) && dispenseItem is not null)
        {
            dbContext.PrescriptionDispenseBatches.Add(new PrescriptionDispenseBatch
            {
                PrescriptionDispenseItemId = dispenseItem.Id,
                StockBatchId = stockBatch.Id,
                BatchNumber = stockBatch.BatchNumber,
                QuantityDispensed = 2
            });
        }

        var pharmacyOrder = await dbContext.PharmacyOrders.FirstOrDefaultAsync(cancellationToken);
        if (pharmacyOrder is null)
        {
            pharmacyOrder = new PharmacyOrder
            {
                TenantId = tenant.Id,
                OrderNumber = $"PHO-SEED-{DateTime.UtcNow:yyyyMMddHHmm}",
                PatientId = patient.Id,
                PrescriptionId = prescription?.Id,
                Status = PharmacyOrderStatus.AssignedForDelivery,
                DeliveryAgentUserId = deliveryAgentUser.Id,
                AssignedAt = DateTimeOffset.UtcNow,
                DeliveryAddress = "Seed Street, Karachi",
                SubTotal = medicine.UnitPrice * 3,
                DeliveryFee = 150,
                TotalAmount = medicine.UnitPrice * 3 + 150
            };
            dbContext.PharmacyOrders.Add(pharmacyOrder);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.PharmacyOrderItems.AnyAsync(cancellationToken))
        {
            dbContext.PharmacyOrderItems.Add(new PharmacyOrderItem
            {
                PharmacyOrderId = pharmacyOrder.Id,
                MedicineId = medicine.Id,
                MedicineName = medicine.BrandName,
                Quantity = 3,
                UnitPrice = medicine.UnitPrice,
                LineTotal = medicine.UnitPrice * 3
            });
        }

        if (!await dbContext.DoctorReviews.AnyAsync(cancellationToken) && appointment is not null)
        {
            dbContext.DoctorReviews.Add(new DoctorReview
            {
                AppointmentId = appointment.Id,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Rating = 5,
                ReviewText = "Seeded review",
                IsRecommended = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> EnsureUserAsync(
        string email,
        string firstName,
        string lastName,
        Guid roleId,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            TenantId = tenantId,
            RoleId = roleId,
            FirstName = firstName,
            LastName = lastName,
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash("ChangeMe@12345"),
            IsActive = true,
            IsEmailVerified = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string ToModule(string permissionKey)
    {
        var first = permissionKey.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "System";
        return char.ToUpperInvariant(first[0]) + first[1..];
    }

    private static string ToAction(string permissionKey)
    {
        var last = permissionKey.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "*";
        return last.Length == 0 ? "*" : char.ToUpperInvariant(last[0]) + last[1..];
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

    private string BuildSeedMeetingLink()
    {
        var baseUrl = configuration[$"{ApplicationLinkOptions.SectionName}:ClientBaseUrl"]?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl)
            ? "https://meeting.seed.local/consultation"
            : $"{baseUrl}/consultation/waiting-room/seed-demo";
    }
}

public static class DatabaseSeederExtensions
{
    public static async Task SeedHealthCareDatabaseAsync(
        this IServiceProvider serviceProvider,
        bool seedDemoData,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HealthCareDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(seedDemoData, cancellationToken);
    }
}
