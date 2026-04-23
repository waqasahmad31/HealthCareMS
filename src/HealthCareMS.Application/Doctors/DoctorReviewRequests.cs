namespace HealthCareMS.Application.Doctors;

public sealed record SubmitDoctorReviewRequest(
    byte Rating,
    string? ReviewText,
    bool IsRecommended);
