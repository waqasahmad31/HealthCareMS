using System.Text.Json;
using HealthCareMS.Application.Doctors;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Doctors;

public sealed partial class DoctorService
{
    public async Task<IReadOnlyList<DoctorRecommendationResponse>> GetRecommendationsAsync(
        DoctorRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var query = DoctorQuery().Where(x => x.IsActive && x.IsVerified);
        if (!string.IsNullOrWhiteSpace(request.Specialization))
        {
            var specialization = request.Specialization.Trim().ToLowerInvariant();
            query = query.Where(x => x.Specialization.ToLower().Contains(specialization));
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLowerInvariant();
            query = query.Where(x => x.City.ToLower().Contains(city));
        }

        if (request.MaxFee.HasValue)
        {
            query = query.Where(x => x.ConsultationFee <= request.MaxFee.Value);
        }

        var doctors = await query
            .OrderByDescending(x => x.AverageRating)
            .ThenBy(x => x.ConsultationFee)
            .Take(100)
            .ToListAsync(cancellationToken);

        var historySignals = await LoadPatientHistorySignalsAsync(request.PatientId, cancellationToken);
        var recommendations = doctors
            .Select(doctor => BuildRecommendation(doctor, request, historySignals))
            .OrderByDescending(x => x.MatchScore)
            .ThenByDescending(x => x.AvailableSlotCount)
            .ThenByDescending(x => x.Doctor.AverageRating)
            .ThenBy(x => x.Doctor.ConsultationFee)
            .ToList();

        for (var i = 0; i < recommendations.Count; i++)
        {
            recommendations[i] = recommendations[i] with { IsBestMatch = i == 0 };
        }

        return recommendations;
    }

    private static DoctorRecommendationResponse BuildRecommendation(
        Domain.Doctors.Doctor doctor,
        DoctorRecommendationRequest request,
        HashSet<string> historySignals)
    {
        var score = 0m;
        var reasons = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Specialization)
            && doctor.Specialization.Contains(request.Specialization, StringComparison.OrdinalIgnoreCase))
        {
            score += 35;
            reasons.Add($"Specialization match: {doctor.Specialization}");
        }

        if (!string.IsNullOrWhiteSpace(request.City)
            && doctor.City.Contains(request.City, StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
            reasons.Add($"City match: {doctor.City}");
        }

        if (request.MaxFee.HasValue)
        {
            var feeScore = request.MaxFee.Value == 0m
                ? 0m
                : Math.Max(0m, 15m - ((doctor.ConsultationFee / request.MaxFee.Value) * 5m));
            score += feeScore;
            if (feeScore > 0m)
            {
                reasons.Add($"Within fee range: {doctor.ConsultationFee:0.00}");
            }
        }

        var ratingScore = Math.Min(20m, doctor.AverageRating * 4m);
        score += ratingScore;
        if (ratingScore > 0m)
        {
            reasons.Add($"Rating strength: {doctor.AverageRating:0.0}/5");
        }

        var slotCount = CountAvailableSlots(doctor, request.Date, request.AppointmentType);
        if (slotCount > 0)
        {
            score += 20m;
            reasons.Add($"Availability found: {slotCount} slots");
        }

        var specializationSignals = historySignals.Where(signal =>
            doctor.Specialization.Contains(signal, StringComparison.OrdinalIgnoreCase)).ToList();
        if (specializationSignals.Count > 0)
        {
            score += 15m;
            reasons.Add($"History-aligned expertise: {string.Join(", ", specializationSignals)}");
        }

        score += Math.Min(10m, doctor.RatingCount);
        return new DoctorRecommendationResponse(
            Map(doctor),
            decimal.Round(score, 2),
            false,
            slotCount,
            reasons);
    }

    private async Task<HashSet<string>> LoadPatientHistorySignalsAsync(Guid? patientId, CancellationToken cancellationToken)
    {
        if (!patientId.HasValue)
        {
            return [];
        }

        var patient = await dbContext.Patients
            .Include(x => x.MedicalHistory)
            .SingleOrDefaultAsync(x => x.Id == patientId.Value, cancellationToken);
        if (patient?.MedicalHistory is null)
        {
            return [];
        }

        var rawSignals = string.Join(' ',
            patient.MedicalHistory.Allergies,
            patient.MedicalHistory.ChronicDiseases,
            patient.MedicalHistory.FamilyHistory).ToLowerInvariant();

        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSignal(rawSignals, mapped, "diabetes", "Endocrinology");
        AddSignal(rawSignals, mapped, "thyroid", "Endocrinology");
        AddSignal(rawSignals, mapped, "hypertension", "Cardiology");
        AddSignal(rawSignals, mapped, "heart", "Cardiology");
        AddSignal(rawSignals, mapped, "kidney", "Nephrology");
        AddSignal(rawSignals, mapped, "renal", "Nephrology");
        AddSignal(rawSignals, mapped, "asthma", "Pulmonology");
        AddSignal(rawSignals, mapped, "allergy", "Immunology");
        AddSignal(rawSignals, mapped, "pregnan", "Gynecology");

        return mapped;
    }

    private static void AddSignal(string raw, ISet<string> signals, string keyword, string specialization)
    {
        if (raw.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            signals.Add(specialization);
        }
    }

    private static int CountAvailableSlots(Domain.Doctors.Doctor doctor, DateOnly? date, string appointmentType)
    {
        if (!date.HasValue)
        {
            return doctor.Schedules.Count;
        }

        var targetType = appointmentType.Trim();
        return doctor.Schedules
            .Where(x => x.DayOfWeek == date.Value.DayOfWeek)
            .Count(x => string.Equals(targetType, "OnSite", StringComparison.OrdinalIgnoreCase)
                ? x.IsOnSiteAvailable
                : x.IsOnlineAvailable);
    }
}
