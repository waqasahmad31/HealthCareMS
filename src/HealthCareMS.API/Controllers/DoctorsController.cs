using HealthCareMS.API.Security;
using HealthCareMS.Application.Doctors;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/doctors")]
public sealed class DoctorsController(
    IDoctorService doctorService,
    IDoctorReviewService doctorReviewService) : ApiControllerBase
{
    [HttpPost("profile")]
    [RequirePermission(PermissionKeys.Doctor.Verify)]
    public async Task<IActionResult> CreateProfile(CreateDoctorProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await doctorService.CreateProfileAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpGet]
    [OutputCache(PolicyName = "LookupGetMedium")]
    public async Task<IActionResult> Search(
        [FromQuery] string? specialization,
        [FromQuery] string? city,
        [FromQuery] decimal? maxFee,
        CancellationToken cancellationToken)
    {
        var doctors = await doctorService.SearchAsync(specialization, city, maxFee, cancellationToken);
        return OkEnvelope(doctors);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [OutputCache(PolicyName = "LookupGetMedium")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await doctorService.GetByIdAsync(id, cancellationToken);
        return FromResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/reviews")]
    [OutputCache(PolicyName = "LookupGetMedium")]
    public async Task<IActionResult> GetReviews(Guid id, CancellationToken cancellationToken)
    {
        var reviews = await doctorReviewService.GetDoctorReviewsAsync(id, cancellationToken);
        return OkEnvelope(reviews);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/rating")]
    [OutputCache(PolicyName = "LookupGetMedium")]
    public async Task<IActionResult> GetRating(Guid id, CancellationToken cancellationToken)
    {
        var result = await doctorReviewService.GetDoctorRatingSummaryAsync(id, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpPost("reviews/appointments/{appointmentId:guid}")]
    public async Task<IActionResult> SubmitReview(
        Guid appointmentId,
        SubmitDoctorReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await doctorReviewService.SubmitReviewAsync(appointmentId, request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}/profile")]
    [RequirePermission(PermissionKeys.Doctor.ScheduleManage)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        UpdateDoctorProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await doctorService.UpdateProfileAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("{id:guid}/verify")]
    [RequirePermission(PermissionKeys.Doctor.Verify)]
    public async Task<IActionResult> Verify(
        Guid id,
        VerifyDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await doctorService.VerifyAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("{id:guid}/schedule")]
    [RequirePermission(PermissionKeys.Doctor.ScheduleManage)]
    public async Task<IActionResult> SetSchedule(
        Guid id,
        SetDoctorScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await doctorService.SetScheduleAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/available-slots")]
    [OutputCache(PolicyName = "LookupGetMedium")]
    public async Task<IActionResult> GetAvailableSlots(
        Guid id,
        [FromQuery] DateOnly date,
        [FromQuery] string appointmentType,
        CancellationToken cancellationToken)
    {
        var result = await doctorService.GetAvailableSlotsAsync(id, date, appointmentType, cancellationToken);
        return FromResult(result);
    }
}
