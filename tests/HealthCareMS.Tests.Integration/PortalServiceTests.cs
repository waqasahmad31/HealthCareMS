using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Infrastructure.Portals;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Tests.Integration;

public sealed class PortalServiceTests
{
    [Fact]
    public async Task GetDoctorDashboardAsync_ShouldReturnScheduleTodayQueueUpcomingAndStats()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedPortalSetupAsync(dbContext);
        var service = new PortalService(dbContext);

        var result = await service.GetDoctorDashboardAsync(setup.DoctorId, setup.Today, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sara Ahmed", result.Value.DoctorName);
        Assert.Equal(3, result.Value.TodayAppointmentCount);
        Assert.Equal(1, result.Value.TodayCompletedCount);
        Assert.Equal(1, result.Value.WaitingQueueCount);
        Assert.Equal(1, result.Value.InProgressCount);
        Assert.True(result.Value.UpcomingAppointmentCount >= 2);
        Assert.Equal(2, result.Value.UniquePatientCount);
        Assert.Equal(2200m, result.Value.TodayConsultationFeeTotal);
        Assert.Single(result.Value.MySchedule);
        Assert.Contains(result.Value.TodayAppointments, x => x.AppointmentNumber == "APT-PORTAL-WAIT");
        Assert.Contains(result.Value.UpcomingAppointments, x => x.AppointmentNumber == "APT-PORTAL-FUTURE");
    }

    [Fact]
    public async Task GetPatientDashboardAndDetailAsync_ShouldReturnMyAppointmentsAndPrescriptionSummary()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedPortalSetupAsync(dbContext);
        var service = new PortalService(dbContext);

        var dashboard = await service.GetPatientDashboardAsync(setup.PatientId, CancellationToken.None);
        var appointments = await service.GetPatientAppointmentsAsync(setup.PatientId, "Completed", CancellationToken.None);
        var detail = await service.GetPatientAppointmentDetailAsync(setup.PatientId, setup.CompletedAppointmentId, CancellationToken.None);

        Assert.True(dashboard.IsSuccess);
        Assert.Equal("Ayesha Khan", dashboard.Value.PatientName);
        Assert.True(dashboard.Value.UpcomingCount >= 1);
        Assert.True(dashboard.Value.PastCount >= 1);
        Assert.Contains("Book Appointment", dashboard.Value.QuickActions);
        Assert.Contains("Track Queue Status", dashboard.Value.QuickActions);
        Assert.Contains("View Prescription", dashboard.Value.QuickActions);
        Assert.NotNull(dashboard.Value.NextAppointment);

        Assert.True(appointments.IsSuccess);
        Assert.Single(appointments.Value);
        Assert.Equal("Completed", appointments.Value[0].Status);

        Assert.True(detail.IsSuccess);
        Assert.Equal("APT-PORTAL-COMPLETE", detail.Value.AppointmentNumber);
        Assert.Equal("Essential hypertension", detail.Value.Diagnosis);
        Assert.Equal("I10", detail.Value.Icd10Code);
        Assert.NotNull(detail.Value.Prescription);
        Assert.Equal(2, detail.Value.Prescription.ItemCount);
        Assert.StartsWith("RX-", detail.Value.Prescription.PrescriptionNumber, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPatientHistoryForDoctorAsync_ShouldReturnMedicalHistoryAndRecentVisits()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedPortalSetupAsync(dbContext);
        var service = new PortalService(dbContext);

        var result = await service.GetPatientHistoryForDoctorAsync(setup.DoctorId, setup.PatientId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ayesha Khan", result.Value.PatientName);
        Assert.Equal("B+", result.Value.BloodGroup);
        Assert.Equal("[\"Penicillin\"]", result.Value.Allergies);
        Assert.Equal("[\"Hypertension\"]", result.Value.ChronicDiseases);
        Assert.Equal(3, result.Value.TotalAppointments);
        Assert.Equal(1, result.Value.CompletedConsultations);
        Assert.NotNull(result.Value.LastVisitAt);
        Assert.Contains(result.Value.RecentAppointments, x => x.AppointmentNumber == "APT-PORTAL-WAIT");
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static async Task<PortalSetup> SeedPortalSetupAsync(HealthCareDbContext dbContext)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var secondaryPatientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };

        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Ayesha",
            LastName = "Khan",
            Email = $"ayesha-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var secondaryPatientUser = new ApplicationUser
        {
            Role = secondaryPatientRole,
            RoleId = secondaryPatientRole.Id,
            FirstName = "Bilal",
            LastName = "Malik",
            Email = $"bilal-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Sara",
            LastName = "Ahmed",
            Email = $"dr-sara-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };

        var patient = new Patient
        {
            User = patientUser,
            UserId = patientUser.Id,
            FirstName = patientUser.FirstName,
            LastName = patientUser.LastName,
            DateOfBirth = new DateOnly(1994, 5, 12),
            Gender = Gender.Female,
            BloodGroup = "B+",
            Phone = "+923001234567",
            AddressCity = "Lahore",
            IsActive = true
        };
        var secondaryPatient = new Patient
        {
            User = secondaryPatientUser,
            UserId = secondaryPatientUser.Id,
            FirstName = secondaryPatientUser.FirstName,
            LastName = secondaryPatientUser.LastName,
            DateOfBirth = new DateOnly(1990, 2, 6),
            Gender = Gender.Male,
            BloodGroup = "O+",
            Phone = "+923009876543",
            AddressCity = "Karachi",
            IsActive = true
        };
        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = "PMDC-PORTAL",
            Specialization = "Internal Medicine",
            Qualification = "MBBS, FCPS",
            Biography = "Clinic physician",
            City = "Lahore",
            ConsultationFee = 2200m,
            IsVerified = true,
            IsActive = true
        };

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime.AddDays(1));
        var clinicDayStart = new DateTimeOffset(today.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var waitAt = clinicDayStart.AddHours(1);
        var inProgressAt = clinicDayStart.AddHours(2);
        var completedAt = clinicDayStart.AddMinutes(30);
        var futureAt = clinicDayStart.AddDays(2);

        var schedule = new DoctorSchedule
        {
            Doctor = doctor,
            DoctorId = doctor.Id,
            DayOfWeek = today.DayOfWeek,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0),
            SlotDurationMinutes = 30,
            IsOnlineAvailable = true,
            IsOnSiteAvailable = true
        };
        doctor.Schedules.Add(schedule);

        var medicalHistory = new MedicalHistory
        {
            Patient = patient,
            PatientId = patient.Id,
            Allergies = "[\"Penicillin\"]",
            ChronicDiseases = "[\"Hypertension\"]",
            CurrentMedications = "[\"Amlodipine\"]",
            PastSurgeries = "[]",
            FamilyHistory = "[\"Diabetes\"]"
        };

        var waiting = CreateAppointment(
            "APT-PORTAL-WAIT",
            patient,
            doctor,
            waitAt,
            AppointmentType.OnSite,
            AppointmentStatus.Confirmed,
            "Patient has a follow up concern",
            doctor.ConsultationFee,
            queueNumber: 4,
            checkedInAt: now.AddMinutes(-10));
        var inProgress = CreateAppointment(
            "APT-PORTAL-INPROGRESS",
            secondaryPatient,
            doctor,
            inProgressAt,
            AppointmentType.OnSite,
            AppointmentStatus.InProgress,
            "Patient needs a quick consultation",
            doctor.ConsultationFee,
            queueNumber: 5,
            checkedInAt: now.AddMinutes(-5));
        var completed = CreateAppointment(
            "APT-PORTAL-COMPLETE",
            patient,
            doctor,
            completedAt,
            AppointmentType.OnSite,
            AppointmentStatus.Completed,
            "Blood pressure review",
            doctor.ConsultationFee,
            queueNumber: 2,
            checkedInAt: completedAt.AddMinutes(-10),
            diagnosis: "Essential hypertension",
            icd10Code: "I10",
            icd10Title: "Essential (primary) hypertension");
        var future = CreateAppointment(
            "APT-PORTAL-FUTURE",
            patient,
            doctor,
            futureAt,
            AppointmentType.Online,
            AppointmentStatus.Pending,
            "Patient booked online follow up",
            doctor.ConsultationFee);

        var prescription = new Prescription
        {
            PrescriptionNumber = $"RX-{now.UtcDateTime:yyyyMMdd}-000001",
            Appointment = completed,
            AppointmentId = completed.Id,
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            Diagnosis = "Essential hypertension",
            Icd10Code = "I10",
            Icd10Title = "Essential (primary) hypertension",
            ClinicalNotes = "Continue medication and monitor BP.",
            FollowUpDate = today.AddDays(14),
            IssuedAt = completedAt,
            ValidUntil = completedAt.AddDays(30),
            Status = PrescriptionStatus.Issued
        };
        prescription.Items.Add(new PrescriptionItem
        {
            Prescription = prescription,
            PrescriptionId = prescription.Id,
            SortOrder = 1,
            MedicineName = "Amlodipine",
            GenericName = "Amlodipine",
            Strength = "5mg",
            Route = "Oral",
            Dosage = "1 tablet",
            Frequency = "OD",
            DurationDays = 30,
            Quantity = 30,
            Instructions = "Morning"
        });
        prescription.Items.Add(new PrescriptionItem
        {
            Prescription = prescription,
            PrescriptionId = prescription.Id,
            SortOrder = 2,
            MedicineName = "Losartan",
            GenericName = "Losartan potassium",
            Strength = "50mg",
            Route = "Oral",
            Dosage = "1 tablet",
            Frequency = "OD",
            DurationDays = 30,
            Quantity = 30,
            Instructions = "Night"
        });

        dbContext.Roles.AddRange(patientRole, doctorRole, secondaryPatientRole);
        dbContext.Users.AddRange(patientUser, doctorUser, secondaryPatientUser);
        dbContext.Patients.AddRange(patient, secondaryPatient);
        dbContext.Doctors.Add(doctor);
        dbContext.MedicalHistories.Add(medicalHistory);
        dbContext.Appointments.AddRange(waiting, inProgress, completed, future);
        dbContext.Prescriptions.Add(prescription);
        await dbContext.SaveChangesAsync();

        return new PortalSetup(today, doctor.Id, patient.Id, completed.Id);
    }

    private static Appointment CreateAppointment(
        string appointmentNumber,
        Patient patient,
        Doctor doctor,
        DateTimeOffset scheduledAt,
        AppointmentType type,
        AppointmentStatus status,
        string reasonForVisit,
        decimal consultationFee,
        int? queueNumber = null,
        DateTimeOffset? checkedInAt = null,
        string? diagnosis = null,
        string? icd10Code = null,
        string? icd10Title = null)
    {
        return new Appointment
        {
            AppointmentNumber = appointmentNumber,
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            ScheduledAt = scheduledAt,
            EndAt = scheduledAt.AddMinutes(30),
            DurationMinutes = 30,
            Type = type,
            Status = status,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = reasonForVisit,
            ConsultationFee = consultationFee,
            QueueNumber = queueNumber,
            CheckedInAt = checkedInAt,
            Diagnosis = diagnosis,
            Icd10Code = icd10Code,
            Icd10Title = icd10Title,
            ClinicalNotes = diagnosis is null ? null : "Patient advised lifestyle changes."
        };
    }

    private sealed record PortalSetup(DateOnly Today, Guid DoctorId, Guid PatientId, Guid CompletedAppointmentId);
}
