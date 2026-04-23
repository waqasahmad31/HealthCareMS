namespace HealthCareMS.Blazor.Models;

public sealed class SubmitDoctorReviewModel
{
    public byte Rating { get; set; } = 5;

    public string? ReviewText { get; set; }

    public bool IsRecommended { get; set; } = true;
}

public sealed record DoctorReviewModel(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    byte Rating,
    string? ReviewText,
    bool IsRecommended,
    DateTimeOffset ReviewedAt,
    DateTimeOffset CreatedAt);

public sealed record DoctorRatingSummaryModel(
    Guid DoctorId,
    string DoctorName,
    decimal AverageRating,
    int RatingCount,
    IReadOnlyList<DoctorReviewModel> RecentReviews);
