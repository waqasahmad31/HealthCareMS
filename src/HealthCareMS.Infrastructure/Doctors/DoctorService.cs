using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Application.Doctors;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Caching;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Doctors;

public sealed partial class DoctorService(
    HealthCareDbContext dbContext,
    IPasswordHasher passwordHasher,
    IDistributedQueryCache queryCache) : IDoctorService
{
    public async Task<Result<DoctorResponse>> CreateProfileAsync(CreateDoctorProfileRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateProfile(request);
        if (validationErrors.Count > 0)
        {
            return Result<DoctorResponse>.Failure(Error.Validation(validationErrors));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (emailExists)
        {
            return Result<DoctorResponse>.Failure(new Error("DOCTOR_EMAIL_EXISTS", "A user with this email already exists."));
        }

        var registrationNumber = request.PmdcRegistrationNumber.Trim();
        var registrationExists = await dbContext.Doctors
            .AnyAsync(x => x.PmdcRegistrationNumber == registrationNumber, cancellationToken);

        if (registrationExists)
        {
            return Result<DoctorResponse>.Failure(new Error("DOCTOR_PMDC_EXISTS", "A doctor with this PMDC registration number already exists."));
        }

        if (request.TenantId.HasValue)
        {
            var tenantExists = await dbContext.Tenants.AnyAsync(x => x.Id == request.TenantId.Value, cancellationToken);
            if (!tenantExists)
            {
                return Result<DoctorResponse>.Failure(new Error("TENANT_NOT_FOUND", "Tenant was not found."));
            }
        }

        var doctorRole = await dbContext.Roles
            .SingleOrDefaultAsync(x => x.TenantId == null && x.Name == "Doctor", cancellationToken);

        if (doctorRole is null)
        {
            return Result<DoctorResponse>.Failure(new Error("DOCTOR_ROLE_MISSING", "Doctor role is not seeded."));
        }

        var user = new ApplicationUser
        {
            TenantId = request.TenantId,
            RoleId = doctorRole.Id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = Normalize(request.PhoneNumber),
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true,
            IsEmailVerified = true
        };

        var doctor = new Doctor
        {
            User = user,
            TenantId = request.TenantId,
            PmdcRegistrationNumber = registrationNumber,
            Specialization = request.Specialization.Trim(),
            Qualification = Normalize(request.Qualification),
            Biography = Normalize(request.Biography),
            City = request.City.Trim(),
            ConsultationFee = request.ConsultationFee,
            IsActive = true
        };

        dbContext.Doctors.Add(doctor);
        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("doctor-search", cancellationToken);

        return Result<DoctorResponse>.Success(Map(doctor));
    }

    public async Task<Result<DoctorResponse>> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        var doctor = await DoctorQuery()
            .SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);

        return doctor is null
            ? Result<DoctorResponse>.Failure(new Error("DOCTOR_NOT_FOUND", "Doctor was not found."))
            : Result<DoctorResponse>.Success(Map(doctor));
    }

    public async Task<IReadOnlyList<DoctorResponse>> SearchAsync(
        string? specialization,
        string? city,
        decimal? maxFee,
        CancellationToken cancellationToken)
    {
        var specializationKey = string.IsNullOrWhiteSpace(specialization) ? "any" : specialization.Trim().ToLowerInvariant();
        var cityKey = string.IsNullOrWhiteSpace(city) ? "any" : city.Trim().ToLowerInvariant();
        var feeKey = maxFee?.ToString("0.##") ?? "any";
        var cacheKey = $"{specializationKey}:{cityKey}:{feeKey}";

        return await queryCache.GetOrCreateAsync(
            "doctor-search",
            cacheKey,
            TimeSpan.FromMinutes(5),
            async token =>
            {
                var query = DoctorQuery().Where(x => x.IsActive && x.IsVerified);

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

                if (maxFee.HasValue)
                {
                    query = query.Where(x => x.ConsultationFee <= maxFee.Value);
                }

                var doctors = await query
                    .OrderBy(x => x.ConsultationFee)
                    .ThenBy(x => x.User.LastName)
                    .Take(100)
                    .ToListAsync(token);

                return (IReadOnlyList<DoctorResponse>)doctors.Select(Map).ToList();
            },
            cancellationToken);
    }

    public async Task<Result<DoctorResponse>> UpdateProfileAsync(
        Guid doctorId,
        UpdateDoctorProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateUpdateProfile(request);
        if (validationErrors.Count > 0)
        {
            return Result<DoctorResponse>.Failure(Error.Validation(validationErrors));
        }

        var doctor = await DoctorQuery()
            .SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);

        if (doctor is null)
        {
            return Result<DoctorResponse>.Failure(new Error("DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        doctor.Specialization = request.Specialization.Trim();
        doctor.Qualification = Normalize(request.Qualification);
        doctor.Biography = Normalize(request.Biography);
        doctor.City = request.City.Trim();
        doctor.ConsultationFee = request.ConsultationFee;
        doctor.IsActive = request.IsActive;
        doctor.User.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("doctor-search", cancellationToken);

        return Result<DoctorResponse>.Success(Map(doctor));
    }

    public async Task<Result<DoctorResponse>> VerifyAsync(
        Guid doctorId,
        VerifyDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var doctor = await DoctorQuery()
            .SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);

        if (doctor is null)
        {
            return Result<DoctorResponse>.Failure(new Error("DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        doctor.IsVerified = request.IsVerified;
        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("doctor-search", cancellationToken);

        return Result<DoctorResponse>.Success(Map(doctor));
    }

    public async Task<Result<IReadOnlyList<DoctorScheduleResponse>>> SetScheduleAsync(
        Guid doctorId,
        SetDoctorScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateSchedule(request);
        if (validationErrors.Count > 0)
        {
            return Result<IReadOnlyList<DoctorScheduleResponse>>.Failure(Error.Validation(validationErrors));
        }

        var doctorExists = await dbContext.Doctors
            .AnyAsync(x => x.Id == doctorId, cancellationToken);

        if (!doctorExists)
        {
            return Result<IReadOnlyList<DoctorScheduleResponse>>.Failure(new Error("DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        var existingSchedules = await dbContext.DoctorSchedules
            .Where(x => x.DoctorId == doctorId)
            .ToListAsync(cancellationToken);

        dbContext.DoctorSchedules.RemoveRange(existingSchedules);
        var activeSchedules = new List<DoctorSchedule>();

        foreach (var slot in request.Slots)
        {
            _ = Enum.TryParse<DayOfWeek>(slot.DayOfWeek, ignoreCase: true, out var dayOfWeek);
            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = dayOfWeek,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                SlotDurationMinutes = slot.SlotDurationMinutes,
                IsOnlineAvailable = slot.IsOnlineAvailable,
                IsOnSiteAvailable = slot.IsOnSiteAvailable
            };

            activeSchedules.Add(schedule);
        }

        dbContext.DoctorSchedules.AddRange(activeSchedules);
        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("doctor-search", cancellationToken);

        return Result<IReadOnlyList<DoctorScheduleResponse>>.Success(activeSchedules.Select(Map).ToList());
    }

    public async Task<Result<IReadOnlyList<AvailableSlotResponse>>> GetAvailableSlotsAsync(
        Guid doctorId,
        DateOnly date,
        string appointmentType,
        CancellationToken cancellationToken)
    {
        if (date < DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            return Result<IReadOnlyList<AvailableSlotResponse>>.Failure(new Error("DOCTOR_SLOT_DATE_INVALID", "Date cannot be in the past."));
        }

        var normalizedType = appointmentType.Trim();
        if (!string.Equals(normalizedType, "Online", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedType, "OnSite", StringComparison.OrdinalIgnoreCase))
        {
            return Result<IReadOnlyList<AvailableSlotResponse>>.Failure(new Error("DOCTOR_SLOT_TYPE_INVALID", "AppointmentType must be Online or OnSite."));
        }

        var doctor = await DoctorQuery()
            .SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);

        if (doctor is null)
        {
            return Result<IReadOnlyList<AvailableSlotResponse>>.Failure(new Error("DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        var schedules = doctor.Schedules
            .Where(x => x.DayOfWeek == date.DayOfWeek)
            .Where(x => string.Equals(normalizedType, "Online", StringComparison.OrdinalIgnoreCase)
                ? x.IsOnlineAvailable
                : x.IsOnSiteAvailable)
            .OrderBy(x => x.StartTime)
            .ToList();

        var slots = new List<AvailableSlotResponse>();
        foreach (var schedule in schedules)
        {
            var cursor = schedule.StartTime;
            while (cursor.AddMinutes(schedule.SlotDurationMinutes) <= schedule.EndTime)
            {
                var end = cursor.AddMinutes(schedule.SlotDurationMinutes);
                slots.Add(new AvailableSlotResponse(cursor, end, normalizedType));
                cursor = end;
            }
        }

        return Result<IReadOnlyList<AvailableSlotResponse>>.Success(slots);
    }

    private IQueryable<Doctor> DoctorQuery()
    {
        return dbContext.Doctors
            .Include(x => x.User)
            .Include(x => x.Schedules);
    }

    private static DoctorResponse Map(Doctor doctor)
    {
        return new DoctorResponse(
            doctor.Id,
            doctor.UserId,
            doctor.TenantId,
            doctor.User.FullName,
            doctor.User.Email,
            doctor.User.PhoneNumber,
            doctor.PmdcRegistrationNumber,
            doctor.Specialization,
            doctor.Qualification,
            doctor.Biography,
            doctor.City,
            doctor.ConsultationFee,
            doctor.AverageRating,
            doctor.RatingCount,
            doctor.IsVerified,
            doctor.IsActive,
            doctor.CreatedAt,
            doctor.Schedules.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).Select(Map).ToList());
    }

    private static DoctorScheduleResponse Map(DoctorSchedule schedule)
    {
        return new DoctorScheduleResponse(
            schedule.Id,
            schedule.DayOfWeek.ToString(),
            schedule.StartTime,
            schedule.EndTime,
            schedule.SlotDurationMinutes,
            schedule.IsOnlineAvailable,
            schedule.IsOnSiteAvailable);
    }

    private static List<ValidationError> ValidateCreateProfile(CreateDoctorProfileRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.FirstName, nameof(request.FirstName), errors);
        Required(request.LastName, nameof(request.LastName), errors);
        Required(request.Email, nameof(request.Email), errors);
        Required(request.Password, nameof(request.Password), errors);
        Required(request.PmdcRegistrationNumber, nameof(request.PmdcRegistrationNumber), errors);
        Required(request.Specialization, nameof(request.Specialization), errors);
        Required(request.City, nameof(request.City), errors);

        if (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 8)
        {
            errors.Add(new ValidationError(nameof(request.Password), "Password must be at least 8 characters."));
        }

        if (request.ConsultationFee < 0)
        {
            errors.Add(new ValidationError(nameof(request.ConsultationFee), "ConsultationFee cannot be negative."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateUpdateProfile(UpdateDoctorProfileRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.Specialization, nameof(request.Specialization), errors);
        Required(request.City, nameof(request.City), errors);

        if (request.ConsultationFee < 0)
        {
            errors.Add(new ValidationError(nameof(request.ConsultationFee), "ConsultationFee cannot be negative."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateSchedule(SetDoctorScheduleRequest request)
    {
        var errors = new List<ValidationError>();
        if (request.Slots is null || request.Slots.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.Slots), "At least one schedule slot is required."));
            return errors;
        }

        for (var i = 0; i < request.Slots.Count; i++)
        {
            var slot = request.Slots[i];
            var prefix = $"{nameof(request.Slots)}[{i}]";

            if (!Enum.TryParse<DayOfWeek>(slot.DayOfWeek, ignoreCase: true, out _))
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(slot.DayOfWeek)}", "DayOfWeek is invalid."));
            }

            if (slot.EndTime <= slot.StartTime)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(slot.EndTime)}", "EndTime must be after StartTime."));
            }

            if (slot.SlotDurationMinutes is < 5 or > 240)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(slot.SlotDurationMinutes)}", "SlotDurationMinutes must be between 5 and 240."));
            }

            if (!slot.IsOnlineAvailable && !slot.IsOnSiteAvailable)
            {
                errors.Add(new ValidationError(prefix, "At least one appointment type must be available."));
            }
        }

        return errors;
    }

    private static void Required(string? value, string field, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(field, $"{field} is required."));
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
