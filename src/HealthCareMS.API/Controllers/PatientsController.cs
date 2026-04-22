using HealthCareMS.API.Security;
using HealthCareMS.Application.Patients;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/patients")]
public sealed class PatientsController(IPatientService patientService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        var result = await patientService.RegisterAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Patient.RecordsViewOthers)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await patientService.GetByIdAsync(id, cancellationToken);
        return FromResult(result);
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Patient.RecordsViewOthers)]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var patients = await patientService.SearchAsync(search, cancellationToken);
        return OkEnvelope(patients);
    }

    [HttpPut("{id:guid}/profile")]
    [RequirePermission(PermissionKeys.Patient.RecordsViewOthers)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        UpdatePatientProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await patientService.UpdateProfileAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("{id:guid}/medical-history")]
    [RequirePermission(PermissionKeys.Patient.RecordsViewOthers)]
    public async Task<IActionResult> UpdateMedicalHistory(
        Guid id,
        UpdateMedicalHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await patientService.UpdateMedicalHistoryAsync(id, request, cancellationToken);
        return FromResult(result);
    }
}
