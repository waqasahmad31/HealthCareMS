using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Doctors;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Doctors;

public sealed class DoctorReviewService(
    HealthCareDbContext dbContext,
    ICurrentUser? currentUser = null) : IDoctorReviewService
{
    public async Task<Result<DoctorReviewResponse>> SubmitReviewAsync(
        Guid appointmentId,
        SubmitDoctorReviewRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result<DoctorReviewResponse>.Failure(Error.Validation(validationErrors));
        }

        var appointment = await dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<DoctorReviewResponse>.Failure(new Error("DOCTOR_REVIEW_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        if (appointment.Status != AppointmentStatus.Completed)
        {
            return Result<DoctorReviewResponse>.Failure(new Error("DOCTOR_REVIEW_APPOINTMENT_INVALID", "Only completed consultations can be reviewed."));
        }

        if (currentUser?.IsAuthenticated == true
            && !currentUser.IsSuperAdmin
            && currentUser.UserId != appointment.Patient.UserId)
        {
            return Result<DoctorReviewResponse>.Failure(new Error("DOCTOR_REVIEW_FORBIDDEN", "Only the appointment patient can submit this review."));
        }

        var exists = await dbContext.DoctorReviews.AnyAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        if (exists)
        {
            return Result<DoctorReviewResponse>.Failure(new Error("DOCTOR_REVIEW_EXISTS", "This consultation has already been reviewed."));
        }

        var review = new DoctorReview
        {
            AppointmentId = appointment.Id,
            Appointment = appointment,
            PatientId = appointment.PatientId,
            Patient = appointment.Patient,
            DoctorId = appointment.DoctorId,
            Doctor = appointment.Doctor,
            Rating = request.Rating,
            ReviewText = Normalize(request.ReviewText),
            IsRecommended = request.IsRecommended,
            ReviewedAt = DateTimeOffset.UtcNow
        };

        dbContext.DoctorReviews.Add(review);
        ApplyRating(appointment.Doctor, request.Rating);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<DoctorReviewResponse>.Success(Map(review));
    }

    public async Task<IReadOnlyList<DoctorReviewResponse>> GetDoctorReviewsAsync(
        Guid doctorId,
        CancellationToken cancellationToken)
    {
        var reviews = await ReviewQuery()
            .Where(x => x.DoctorId == doctorId)
            .OrderByDescending(x => x.ReviewedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return reviews.Select(Map).ToList();
    }

    public async Task<Result<DoctorRatingSummaryResponse>> GetDoctorRatingSummaryAsync(
        Guid doctorId,
        CancellationToken cancellationToken)
    {
        var doctor = await dbContext.Doctors
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);

        if (doctor is null)
        {
            return Result<DoctorRatingSummaryResponse>.Failure(new Error("DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        var reviews = await GetDoctorReviewsAsync(doctorId, cancellationToken);
        return Result<DoctorRatingSummaryResponse>.Success(new DoctorRatingSummaryResponse(
            doctor.Id,
            doctor.User.FullName,
            doctor.AverageRating,
            doctor.RatingCount,
            reviews.Take(10).ToList()));
    }

    private IQueryable<DoctorReview> ReviewQuery()
    {
        return dbContext.DoctorReviews
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User);
    }

    private static void ApplyRating(Doctor doctor, byte rating)
    {
        var total = doctor.AverageRating * doctor.RatingCount + rating;
        doctor.RatingCount++;
        doctor.AverageRating = Math.Round(total / doctor.RatingCount, 2, MidpointRounding.AwayFromZero);
    }

    private static List<ValidationError> Validate(SubmitDoctorReviewRequest request)
    {
        var errors = new List<ValidationError>();
        if (request.Rating is < 1 or > 5)
        {
            errors.Add(new ValidationError(nameof(request.Rating), "Rating must be between 1 and 5."));
        }

        if (!string.IsNullOrWhiteSpace(request.ReviewText) && request.ReviewText.Trim().Length > 1000)
        {
            errors.Add(new ValidationError(nameof(request.ReviewText), "ReviewText cannot exceed 1000 characters."));
        }

        return errors;
    }

    private static DoctorReviewResponse Map(DoctorReview review)
    {
        return new DoctorReviewResponse(
            review.Id,
            review.AppointmentId,
            review.PatientId,
            $"{review.Patient.FirstName} {review.Patient.LastName}".Trim(),
            review.DoctorId,
            review.Doctor.User.FullName,
            review.Rating,
            review.ReviewText,
            review.IsRecommended,
            review.ReviewedAt,
            review.CreatedAt);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
