using HealthCareMS.API.Security;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Application.Labs;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/consultations")]
public sealed class ConsultationsController(
    IConsultationService consultationService,
    IConsultationSessionService consultationSessionService,
    IConsultationChatService consultationChatService,
    ILabService labService,
    ICurrentUser currentUser) : ApiControllerBase
{
    [HttpPost("sessions")]
    [RequirePermission(PermissionKeys.Consultation.VideoStart)]
    public async Task<IActionResult> StartSession(
        StartConsultationSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationSessionService.StartAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [Authorize]
    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await consultationSessionService.GetByIdAsync(sessionId, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpGet("appointments/{appointmentId:guid}/session")]
    public async Task<IActionResult> GetSessionByAppointment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await consultationSessionService.GetByAppointmentAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpPost("sessions/{sessionId:guid}/join")]
    public async Task<IActionResult> JoinSession(
        Guid sessionId,
        JoinConsultationSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationSessionService.JoinAsync(sessionId, request, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpGet("sessions/{sessionId:guid}/chat/messages")]
    public async Task<IActionResult> GetChatMessages(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await consultationChatService.GetMessagesAsync(sessionId, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpPost("sessions/{sessionId:guid}/chat/messages")]
    public async Task<IActionResult> SendChatMessage(
        Guid sessionId,
        SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationChatService.SendMessageAsync(
            sessionId,
            request,
            currentUser.UserId,
            cancellationToken);

        return FromResult(result, StatusCodes.Status201Created);
    }

    [Authorize]
    [HttpPost("sessions/{sessionId:guid}/chat/attachments")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadChatAttachment(
        Guid sessionId,
        [FromForm] UploadChatAttachmentForm form,
        CancellationToken cancellationToken)
    {
        await using var stream = form.File.OpenReadStream();
        var result = await consultationChatService.UploadAttachmentAsync(
            sessionId,
            new UploadChatAttachmentRequest(
                form.ParticipantType,
                form.SenderDisplayName,
                form.File.FileName,
                form.File.ContentType,
                form.File.Length,
                stream),
            currentUser.UserId,
            cancellationToken);

        return FromResult(result, StatusCodes.Status201Created);
    }

    public sealed class UploadChatAttachmentForm
    {
        public string ParticipantType { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        public IFormFile File { get; set; } = default!;
    }

    [Authorize]
    [HttpPut("sessions/{sessionId:guid}/chat/read")]
    public async Task<IActionResult> MarkChatMessagesRead(
        Guid sessionId,
        MarkChatMessagesReadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationChatService.MarkReadAsync(sessionId, request, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpGet("chat/messages/{messageId:guid}/attachment")]
    public async Task<IActionResult> DownloadChatAttachment(Guid messageId, CancellationToken cancellationToken)
    {
        var result = await consultationChatService.DownloadAttachmentAsync(messageId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPut("appointments/{appointmentId:guid}/complete")]
    [RequirePermission(PermissionKeys.Consultation.PrescriptionCreate)]
    public async Task<IActionResult> CompleteAppointment(
        Guid appointmentId,
        CompleteConsultationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationService.CompleteAsync(appointmentId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("drugs")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> SearchDrapMedicines([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var results = await consultationService.SearchDrapMedicinesAsync(search, cancellationToken);
        return OkEnvelope(results);
    }

    [HttpPost("patients/{patientId:guid}/drug-allergy-check")]
    [RequirePermission(PermissionKeys.Consultation.PrescriptionCreate)]
    public async Task<IActionResult> CheckDrugAllergies(
        Guid patientId,
        DrugAllergyCheckRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationService.CheckDrugAllergiesAsync(patientId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("appointments/{appointmentId:guid}/prescription")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetPrescription(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await consultationService.GetPrescriptionByAppointmentAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("appointments/{appointmentId:guid}/lab-orders")]
    [RequirePermission(PermissionKeys.Lab.BookingCreate)]
    public async Task<IActionResult> CreateLabOrder(
        Guid appointmentId,
        CreateConsultationLabOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.CreateConsultationLabOrderAsync(appointmentId, request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("appointments/{appointmentId:guid}/summary")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetSummary(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await consultationService.GetSummaryAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("appointments/{appointmentId:guid}/summary/pdf")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> DownloadSummaryPdf(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await consultationService.GenerateSummaryPdfAsync(appointmentId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [AllowAnonymous]
    [HttpGet("prescriptions/{prescriptionId:guid}/verify")]
    public async Task<IActionResult> VerifyPrescription(
        Guid prescriptionId,
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        var result = await consultationService.VerifyPrescriptionAsync(prescriptionId, code, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("prescriptions/{prescriptionId:guid}/pdf")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> DownloadPrescriptionPdf(Guid prescriptionId, CancellationToken cancellationToken)
    {
        var result = await consultationService.GeneratePrescriptionPdfAsync(prescriptionId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("icd10")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> SearchIcd10([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var results = await consultationService.SearchIcd10Async(search, cancellationToken);
        return OkEnvelope(results);
    }
}
