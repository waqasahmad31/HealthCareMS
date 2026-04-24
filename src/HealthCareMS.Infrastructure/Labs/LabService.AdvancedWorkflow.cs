using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using HealthCareMS.Application.Labs;
using HealthCareMS.Application.Notifications;
using HealthCareMS.Domain.Labs;
using HealthCareMS.Domain.Notifications;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HealthCareMS.Infrastructure.Labs;

public sealed partial class LabService
{
    public async Task<Result<LabBookingResponse>> AssignCollectionAgentAsync(
        Guid bookingId,
        AssignLabCollectionAgentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CollectionAgentUserId == Guid.Empty)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_COLLECTION_AGENT_INVALID", "CollectionAgentUserId is required."));
        }

        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        if (booking.CollectionType != LabCollectionType.Home)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_COLLECTION_TYPE_INVALID", "Only home collections can be assigned to an agent."));
        }

        var agent = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == request.CollectionAgentUserId, cancellationToken);
        if (agent is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_COLLECTION_AGENT_NOT_FOUND", "Collection agent user was not found."));
        }

        booking.CollectionAgentUserId = request.CollectionAgentUserId;
        booking.CollectionAssignedAt = DateTimeOffset.UtcNow;
        booking.CollectionScheduledAt = request.CollectionScheduledAt?.ToUniversalTime() ?? booking.CollectionScheduledAt;
        booking.CollectionWindowEndAt = request.CollectionWindowEndAt?.ToUniversalTime() ?? booking.CollectionWindowEndAt;
        booking.Status = LabBookingStatus.AgentAssigned;
        booking.CollectionStatusNotes = MergeNotes(booking.CollectionStatusNotes, request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LabBookingResponse>.Success(Map(booking));
    }

    public async Task<Result<IReadOnlyList<LabBookingResponse>>> GetAssignedCollectionsAsync(
        Guid collectionAgentUserId,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = BookingQuery()
            .AsNoTracking()
            .Where(x => x.CollectionType == LabCollectionType.Home && x.CollectionAgentUserId == collectionAgentUserId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<LabBookingStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                return Result<IReadOnlyList<LabBookingResponse>>.Failure(new Error("LAB_BOOKING_STATUS_INVALID", "Status is invalid."));
            }

            query = query.Where(x => x.Status == parsedStatus);
        }

        var items = await query
            .OrderBy(x => x.CollectionScheduledAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LabBookingResponse>>.Success(items.Select(Map).ToList());
    }

    public async Task<Result<LabBookingResponse>> StartCollectionAsync(
        Guid bookingId,
        StartLabCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        if (booking.CollectionType != LabCollectionType.Home)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_COLLECTION_TYPE_INVALID", "Only home collections can be started."));
        }

        if (booking.CollectionAgentUserId is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_COLLECTION_AGENT_REQUIRED", "A collection agent must be assigned before collection starts."));
        }

        booking.Status = LabBookingStatus.InTransit;
        booking.CollectionStartedAt = DateTimeOffset.UtcNow;
        booking.CollectionStatusNotes = MergeNotes(booking.CollectionStatusNotes, request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LabBookingResponse>.Success(Map(booking));
    }

    public async Task<Result<LabBookingResponse>> MarkSampleCollectedAsync(
        Guid bookingId,
        MarkLabSampleCollectedRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        if (booking.CollectionType == LabCollectionType.Home && booking.CollectionAgentUserId is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_COLLECTION_AGENT_REQUIRED", "Home collection booking must have an assigned agent before marking collected."));
        }

        booking.Status = LabBookingStatus.SampleCollected;
        booking.SampleCollectedAt = DateTimeOffset.UtcNow;
        booking.FastingVerified = request.FastingVerified ?? booking.FastingVerified;
        booking.CollectionStatusNotes = MergeNotes(booking.CollectionStatusNotes, request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LabBookingResponse>.Success(Map(booking));
    }

    public async Task<Result<IReadOnlyList<LabTestResultResponse>>> EnterResultsAsync(
        Guid bookingId,
        EnterLabResultsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Results is null || request.Results.Count == 0)
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_RESULTS_REQUIRED", "At least one lab result is required."));
        }

        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        if (booking.Status == LabBookingStatus.Cancelled)
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_BOOKING_STATUS_INVALID", "Cannot enter results for a cancelled booking."));
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in request.Results)
        {
            var bookingItem = booking.Items.SingleOrDefault(x => x.Id == item.LabBookingItemId);
            if (bookingItem is null)
            {
                return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_BOOKING_ITEM_NOT_FOUND", "One or more booking items were not found."));
            }

            var parameters = item.Parameters?.Select(EvaluateParameter).ToList() ?? [];
            var hasAbnormal = parameters.Any(x => x.IsAbnormal);
            var criticalParameters = parameters.Where(x => x.IsCritical).ToList();
            var result = await dbContext.LabTestResults
                .Include(x => x.LabTest)
                .SingleOrDefaultAsync(x => x.LabBookingItemId == bookingItem.Id, cancellationToken);

            if (result is null)
            {
                result = new LabTestResult
                {
                    LabSampleBookingId = booking.Id,
                    LabSampleBooking = booking,
                    LabBookingItemId = bookingItem.Id,
                    LabBookingItem = bookingItem,
                    LabTestId = bookingItem.LabTestId,
                    LabTest = bookingItem.LabTest,
                    ResultNumber = await GenerateResultNumberAsync(now, cancellationToken)
                };

                dbContext.LabTestResults.Add(result);
            }

            result.ParametersJson = JsonSerializer.Serialize(parameters);
            result.Summary = Normalize(item.Summary);
            result.IsAbnormal = hasAbnormal;
            result.HasCriticalValue = criticalParameters.Count > 0;
            result.CriticalValueSummary = criticalParameters.Count == 0
                ? null
                : string.Join("; ", criticalParameters.Select(x => $"{x.ParameterName}: {x.Value}"));
            result.Status = LabResultStatus.Entered;
            result.AutoValidatedAt = now;
            result.EnteredAt = now;
            result.EnteredByUserId = currentUser?.UserId;

            if (result.HasCriticalValue && result.CriticalAlertSentAt is null)
            {
                await SendCriticalValueAlertAsync(booking, result, cancellationToken);
            }
        }

        booking.Status = LabBookingStatus.ResultsPending;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetResultsAsync(bookingId, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<LabTestResultResponse>>> GetResultsAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var bookingExists = await dbContext.LabSampleBookings.AnyAsync(x => x.Id == bookingId, cancellationToken);
        if (!bookingExists)
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        var results = await ResultQuery()
            .Where(x => x.LabSampleBookingId == bookingId)
            .OrderBy(x => x.LabTest.TestName)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LabTestResultResponse>>.Success(results.Select(Map).ToList());
    }

    public async Task<Result<LabTestResultResponse>> AcknowledgeCriticalAlertAsync(
        Guid resultId,
        AcknowledgeLabCriticalAlertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ResultQuery().SingleOrDefaultAsync(x => x.Id == resultId, cancellationToken);
        if (result is null)
        {
            return Result<LabTestResultResponse>.Failure(new Error("LAB_RESULT_NOT_FOUND", "Lab result was not found."));
        }

        if (!result.HasCriticalValue)
        {
            return Result<LabTestResultResponse>.Failure(new Error("LAB_CRITICAL_ALERT_NOT_FOUND", "This result does not have a critical alert."));
        }

        result.CriticalAlertAcknowledgedAt = DateTimeOffset.UtcNow;
        result.CriticalAlertAcknowledgedByUserId = currentUser?.UserId;
        if (!string.IsNullOrWhiteSpace(request.AcknowledgementNote))
        {
            result.AddendumNotes = MergeNotes(result.AddendumNotes, $"Critical alert acknowledgement: {request.AcknowledgementNote}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<LabTestResultResponse>.Success(Map(result));
    }

    public async Task<Result<IReadOnlyList<LabValidationQueueItemResponse>>> GetValidationQueueAsync(
        string? filter,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(filter)?.ToLowerInvariant();
        var bookings = await BookingQuery()
            .AsNoTracking()
            .Where(x => x.Results.Any())
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var queue = bookings
            .Select(booking =>
            {
                var results = booking.Results.ToList();
                return new LabValidationQueueItemResponse(
                    booking.Id,
                    booking.BookingNumber,
                    booking.PatientId,
                    $"{booking.Patient.FirstName} {booking.Patient.LastName}".Trim(),
                    booking.Status.ToString(),
                    results.Any(x => x.HasCriticalValue),
                    results.Any(x => x.IsAbnormal),
                    results.Count(x => x.Status == LabResultStatus.Entered),
                    results.Count(x => x.Status == LabResultStatus.TechValidated),
                    results.Count(x => x.Status == LabResultStatus.ManagerValidated),
                    booking.CreatedAt);
            })
            .Where(item => normalized switch
            {
                "pending" => item.PendingTechValidationCount > 0 || item.PendingManagerValidationCount > 0,
                "abnormal" => item.HasAbnormalResult,
                "critical" => item.HasCriticalValue,
                _ => true
            })
            .ToList();

        return Result<IReadOnlyList<LabValidationQueueItemResponse>>.Success(queue);
    }

    public async Task<Result<IReadOnlyList<LabTestResultResponse>>> ValidateResultsAsync(
        Guid bookingId,
        ValidateLabResultsRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        if (booking.Results.Count == 0)
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_RESULTS_NOT_FOUND", "Results have not been entered for this booking."));
        }

        var level = Normalize(request.Level);
        if (string.Equals(level, "Tech", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var result in booking.Results.Where(x => x.Status is LabResultStatus.Entered or LabResultStatus.Corrected))
            {
                result.Status = LabResultStatus.TechValidated;
                result.TechnicianValidatedAt = DateTimeOffset.UtcNow;
                result.TechnicianValidatedByUserId = currentUser?.UserId;
            }
        }
        else if (string.Equals(level, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            if (booking.Results.Any(x => x.Status is not (LabResultStatus.TechValidated or LabResultStatus.ManagerValidated or LabResultStatus.Released)))
            {
                return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_RESULTS_VALIDATION_SEQUENCE_INVALID", "Tech validation must complete before manager validation."));
            }

            foreach (var result in booking.Results.Where(x => x.Status is LabResultStatus.TechValidated or LabResultStatus.Corrected))
            {
                result.Status = LabResultStatus.ManagerValidated;
                result.ManagerValidatedAt = DateTimeOffset.UtcNow;
                result.ManagerValidatedByUserId = currentUser?.UserId;
            }
        }
        else
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_RESULTS_VALIDATION_LEVEL_INVALID", "Level must be Tech or Manager."));
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            booking.Notes = MergeNotes(booking.Notes, $"Validation ({level}): {request.Notes}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<IReadOnlyList<LabTestResultResponse>>.Success(booking.Results.OrderBy(x => x.LabTest.TestName).Select(Map).ToList());
    }

    public async Task<Result<IReadOnlyList<LabTestResultResponse>>> ReleaseResultsAsync(
        Guid bookingId,
        ReleaseLabResultsRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        if (booking.Results.Count == 0)
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_RESULTS_NOT_FOUND", "Results have not been entered for this booking."));
        }

        if (booking.Results.Any(x => x.Status is not (LabResultStatus.ManagerValidated or LabResultStatus.Released or LabResultStatus.Corrected)))
        {
            return Result<IReadOnlyList<LabTestResultResponse>>.Failure(new Error("LAB_RESULTS_RELEASE_INVALID", "Manager validation must complete before releasing results."));
        }

        var now = DateTimeOffset.UtcNow;
        booking.Status = LabBookingStatus.ResultsReleased;
        booking.ResultsReleasedAt = now;
        booking.ReportVerificationCode ??= Convert.ToHexString(RandomNumberGenerator.GetBytes(10));
        booking.Notes = MergeNotes(booking.Notes, request.Notes);

        foreach (var result in booking.Results)
        {
            result.Status = LabResultStatus.Released;
            result.ReleasedAt ??= now;
            result.ReleasedByUserId ??= currentUser?.UserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await SendResultReleasedNotificationsAsync(booking, cancellationToken);
        return Result<IReadOnlyList<LabTestResultResponse>>.Success(booking.Results.OrderBy(x => x.LabTest.TestName).Select(Map).ToList());
    }

    public async Task<Result<LabTestResultResponse>> AddAddendumAsync(
        Guid resultId,
        AddLabResultAddendumRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AddendumNotes))
        {
            return Result<LabTestResultResponse>.Failure(new Error("LAB_RESULT_ADDENDUM_REQUIRED", "AddendumNotes is required."));
        }

        var result = await ResultQuery().SingleOrDefaultAsync(x => x.Id == resultId, cancellationToken);
        if (result is null)
        {
            return Result<LabTestResultResponse>.Failure(new Error("LAB_RESULT_NOT_FOUND", "Lab result was not found."));
        }

        result.AddendumNotes = MergeNotes(result.AddendumNotes, request.AddendumNotes);
        result.AddendumAt = DateTimeOffset.UtcNow;
        result.AddendumByUserId = currentUser?.UserId;
        result.Status = LabResultStatus.Corrected;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LabTestResultResponse>.Success(Map(result));
    }

    public async Task<Result<LabReportPdfResponse>> GenerateReportPdfAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<LabReportPdfResponse>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        if (booking.Results.Count == 0 || booking.ResultsReleasedAt is null)
        {
            return Result<LabReportPdfResponse>.Failure(new Error("LAB_REPORT_NOT_READY", "Released results are required before generating a report."));
        }

        booking.ReportVerificationCode ??= Convert.ToHexString(RandomNumberGenerator.GetBytes(10));
        booking.ReportGeneratedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var pdf = GenerateLabReportPdf(booking);
        return Result<LabReportPdfResponse>.Success(new LabReportPdfResponse(
            pdf,
            $"{booking.BookingNumber}-Report.pdf",
            "application/pdf"));
    }

    public async Task<Result<IReadOnlyList<LabBookingResultSummaryResponse>>> GetPatientResultsAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var patientExists = await dbContext.Patients.AnyAsync(x => x.Id == patientId, cancellationToken);
        if (!patientExists)
        {
            return Result<IReadOnlyList<LabBookingResultSummaryResponse>>.Failure(new Error("LAB_PATIENT_NOT_FOUND", "Patient was not found."));
        }

        var bookings = await BookingQuery()
            .Where(x => x.PatientId == patientId && x.Results.Any())
            .OrderByDescending(x => x.ResultsReleasedAt ?? x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var response = bookings.Select(booking => new LabBookingResultSummaryResponse(
            booking.Id,
            booking.BookingNumber,
            booking.Patient.User.FullName,
            booking.Status.ToString(),
            booking.ResultsReleasedAt,
            booking.ReportVerificationCode,
            booking.Results.OrderBy(x => x.LabTest.TestName).Select(Map).ToList())).ToList();

        return Result<IReadOnlyList<LabBookingResultSummaryResponse>>.Success(response);
    }

    public async Task<Result<LabReportVerificationResponse>> VerifyReportAsync(
        Guid bookingId,
        string verificationCode,
        CancellationToken cancellationToken)
    {
        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<LabReportVerificationResponse>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        var isVerified = !string.IsNullOrWhiteSpace(verificationCode)
            && string.Equals(booking.ReportVerificationCode, verificationCode.Trim(), StringComparison.OrdinalIgnoreCase)
            && booking.ResultsReleasedAt is not null;

        return Result<LabReportVerificationResponse>.Success(new LabReportVerificationResponse(
            booking.Id,
            booking.BookingNumber,
            booking.Patient.User.FullName,
            isVerified,
            booking.Status.ToString(),
            booking.ResultsReleasedAt,
            booking.Results.OrderBy(x => x.LabTest.TestName).Select(x => x.LabTest.TestName).ToList()));
    }

    private IQueryable<LabTestResult> ResultQuery()
    {
        return dbContext.LabTestResults
            .Include(x => x.LabSampleBooking)
            .ThenInclude(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.LabBookingItem)
            .ThenInclude(x => x.LabTest)
            .Include(x => x.LabTest);
    }

    private async Task<string> GenerateResultNumberAsync(DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        var prefix = $"LRES-{createdAt:yyyyMMdd}-";
        var count = await dbContext.LabTestResults.CountAsync(x => x.ResultNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private static LabResultParameterSnapshot EvaluateParameter(LabResultParameterRequest request)
    {
        var normalizedName = request.ParameterName.Trim();
        var isNumeric = decimal.TryParse(request.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue);
        var isAbnormal = isNumeric
            && ((request.ReferenceLow.HasValue && numericValue < request.ReferenceLow.Value)
                || (request.ReferenceHigh.HasValue && numericValue > request.ReferenceHigh.Value));
        var isCritical = isNumeric
            && ((request.CriticalLow.HasValue && numericValue < request.CriticalLow.Value)
                || (request.CriticalHigh.HasValue && numericValue > request.CriticalHigh.Value));

        return new LabResultParameterSnapshot(
            normalizedName,
            request.Value.Trim(),
            Normalize(request.Unit),
            request.ReferenceLow,
            request.ReferenceHigh,
            Normalize(request.ReferenceText),
            isAbnormal,
            isCritical,
            Normalize(request.Notes),
            request.CriticalLow,
            request.CriticalHigh);
    }

    private async Task SendCriticalValueAlertAsync(
        LabSampleBooking booking,
        LabTestResult result,
        CancellationToken cancellationToken)
    {
        var recipients = await ResolveAlertRecipientsAsync(booking, cancellationToken);
        if (recipients.Count == 0)
        {
            result.CriticalAlertSentAt = DateTimeOffset.UtcNow;
            return;
        }

        var subject = $"Critical lab value: {result.LabTest.TestName}";
        var body = $"Critical value detected for booking {booking.BookingNumber}. {result.CriticalValueSummary}";

        foreach (var recipient in recipients)
        {
            await CreateNotificationAsync(
                recipient,
                NotificationType.LabCriticalValueAlert,
                subject,
                body,
                "LabBooking",
                booking.Id,
                cancellationToken);
        }

        result.CriticalAlertSentAt = DateTimeOffset.UtcNow;
    }

    private async Task SendResultReleasedNotificationsAsync(LabSampleBooking booking, CancellationToken cancellationToken)
    {
        var recipients = await ResolveReleaseRecipientsAsync(booking, cancellationToken);
        var subject = $"Lab results released: {booking.BookingNumber}";
        var body = $"Released lab results are now available for booking {booking.BookingNumber}.";

        foreach (var recipient in recipients)
        {
            await CreateNotificationAsync(
                recipient,
                NotificationType.LabResultReleased,
                subject,
                body,
                "LabBooking",
                booking.Id,
                cancellationToken);
        }
    }

    private async Task<List<RecipientProfile>> ResolveAlertRecipientsAsync(LabSampleBooking booking, CancellationToken cancellationToken)
    {
        var recipients = new List<RecipientProfile>();
        var patientUser = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == booking.Patient.UserId, cancellationToken);
        if (patientUser is not null)
        {
            recipients.Add(new RecipientProfile(patientUser, booking.Patient.Phone));
        }

        if (booking.AppointmentId.HasValue)
        {
            var doctor = await dbContext.Appointments
                .Include(x => x.Doctor)
                .ThenInclude(x => x.User)
                .Where(x => x.Id == booking.AppointmentId.Value)
                .Select(x => new { x.Doctor.User, x.Doctor.User.PhoneNumber })
                .SingleOrDefaultAsync(cancellationToken);

            if (doctor is not null)
            {
                recipients.Add(new RecipientProfile(doctor.User, doctor.PhoneNumber));
            }
        }

        return recipients
            .GroupBy(x => x.User.Id)
            .Select(x => x.First())
            .ToList();
    }

    private async Task<List<RecipientProfile>> ResolveReleaseRecipientsAsync(LabSampleBooking booking, CancellationToken cancellationToken)
    {
        var recipients = await ResolveAlertRecipientsAsync(booking, cancellationToken);
        return recipients;
    }

    private async Task CreateNotificationAsync(
        RecipientProfile recipient,
        NotificationType type,
        string subject,
        string body,
        string referenceType,
        Guid referenceId,
        CancellationToken cancellationToken)
    {
        var preferences = await dbContext.NotificationPreferences
            .SingleOrDefaultAsync(x => x.UserId == recipient.User.Id, cancellationToken);

        var inAppEnabled = preferences?.InAppEnabled ?? true;
        var emailEnabled = preferences?.EmailEnabled ?? true;
        var smsEnabled = preferences?.SmsEnabled ?? true;

        if (inAppEnabled)
        {
            var notification = new Notification
            {
                RecipientUserId = recipient.User.Id,
                TenantId = recipient.User.TenantId,
                Channel = NotificationChannel.InApp,
                Type = type,
                Status = NotificationStatus.Sent,
                Subject = subject,
                Body = body,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                SentAt = DateTimeOffset.UtcNow
            };

            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (inAppPublisher is not null)
            {
                await inAppPublisher.PublishAsync(new NotificationResponse(
                    notification.Id,
                    notification.RecipientUserId,
                    notification.Channel.ToString(),
                    notification.Type.ToString(),
                    notification.Status.ToString(),
                    notification.Subject,
                    notification.Body,
                    notification.Destination,
                    notification.ReferenceType,
                    notification.ReferenceId,
                    notification.ScheduledAt,
                    notification.SentAt,
                    notification.IsRead,
                    notification.ReadAt,
                    notification.CreatedAt), cancellationToken);
            }
        }

        if (emailEnabled && emailSender is not null)
        {
            var delivery = await emailSender.SendAsync(recipient.User.Email, subject, body, cancellationToken);
            dbContext.Notifications.Add(new Notification
            {
                RecipientUserId = recipient.User.Id,
                TenantId = recipient.User.TenantId,
                Channel = NotificationChannel.Email,
                Type = type,
                Status = delivery.IsSuccess ? NotificationStatus.Sent : NotificationStatus.Failed,
                Subject = subject,
                Body = body,
                Destination = recipient.User.Email,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                SentAt = delivery.IsSuccess ? DateTimeOffset.UtcNow : null,
                FailureReason = delivery.IsSuccess ? null : delivery.FailureReason
            });
        }

        if (smsEnabled && smsSender is not null && !string.IsNullOrWhiteSpace(recipient.PhoneNumber))
        {
            var delivery = await smsSender.SendAsync(recipient.PhoneNumber, body, cancellationToken);
            dbContext.Notifications.Add(new Notification
            {
                RecipientUserId = recipient.User.Id,
                TenantId = recipient.User.TenantId,
                Channel = NotificationChannel.Sms,
                Type = type,
                Status = delivery.IsSuccess ? NotificationStatus.Sent : NotificationStatus.Failed,
                Subject = subject,
                Body = body,
                Destination = recipient.PhoneNumber,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                SentAt = delivery.IsSuccess ? DateTimeOffset.UtcNow : null,
                FailureReason = delivery.IsSuccess ? null : delivery.FailureReason
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static byte[] GenerateLabReportPdf(LabSampleBooking booking)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var verificationLink = $"https://healthcarems.local/lab/verify/{booking.Id}?code={booking.ReportVerificationCode}";
        var qrPng = new PngByteQRCode(new QRCodeGenerator().CreateQrCode(verificationLink, QRCodeGenerator.ECCLevel.Q)).GetGraphic(10);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(column =>
                {
                    column.Spacing(3);
                    column.Item().Text("HealthCareMS Laboratory Report").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Booking: {booking.BookingNumber}");
                    column.Item().Text($"Patient: {booking.Patient.User.FullName}");
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(details =>
                        {
                            details.Item().Text($"Collected: {booking.SampleCollectedAt:yyyy-MM-dd HH:mm} UTC");
                            details.Item().Text($"Released: {booking.ResultsReleasedAt:yyyy-MM-dd HH:mm} UTC");
                            details.Item().Text($"Verification Code: {booking.ReportVerificationCode}");
                        });

                        row.ConstantItem(90).Height(90).Image(qrPng);
                    });

                    foreach (var result in booking.Results.OrderBy(x => x.LabTest.TestName))
                    {
                        column.Item().PaddingTop(6).Text($"{result.LabTest.TestName} ({result.LabTest.TestCode})").Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Parameter");
                                header.Cell().Element(HeaderCell).Text("Value");
                                header.Cell().Element(HeaderCell).Text("Range");
                                header.Cell().Element(HeaderCell).Text("Flag");
                            });

                            foreach (var parameter in DeserializeParameters(result.ParametersJson))
                            {
                                var range = parameter.ReferenceText
                                    ?? (parameter.ReferenceLow.HasValue || parameter.ReferenceHigh.HasValue
                                        ? $"{parameter.ReferenceLow?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-"} - {parameter.ReferenceHigh?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-"}"
                                        : "-");
                                var flag = parameter.IsCritical ? "Critical" : parameter.IsAbnormal ? "Abnormal" : "Normal";
                                table.Cell().Element(BodyCell).Text(parameter.ParameterName);
                                table.Cell().Element(BodyCell).Text($"{parameter.Value} {parameter.Unit}".Trim());
                                table.Cell().Element(BodyCell).Text(range);
                                table.Cell().Element(BodyCell).Text(flag);
                            }
                        });

                        if (!string.IsNullOrWhiteSpace(result.Summary))
                        {
                            column.Item().Text($"Summary: {result.Summary}");
                        }

                        if (!string.IsNullOrWhiteSpace(result.AddendumNotes))
                        {
                            column.Item().Text($"Addendum: {result.AddendumNotes}");
                        }
                    }
                });

                page.Footer().AlignCenter().Text("Authorized by HealthCareMS Laboratory Manager");
            });
        }).GeneratePdf();
    }

    private static LabTestResultResponse Map(LabTestResult result)
    {
        return new LabTestResultResponse(
            result.Id,
            result.LabSampleBookingId,
            result.LabBookingItemId,
            result.LabTestId,
            result.ResultNumber,
            result.LabTest.TestCode,
            result.LabTest.TestName,
            result.Status.ToString(),
            result.Summary,
            result.IsAbnormal,
            result.HasCriticalValue,
            result.CriticalValueSummary,
            result.EnteredAt,
            result.TechnicianValidatedAt,
            result.ManagerValidatedAt,
            result.ReleasedAt,
            result.CriticalAlertSentAt,
            result.CriticalAlertAcknowledgedAt,
            result.AddendumNotes,
            result.AddendumAt,
            DeserializeParameters(result.ParametersJson).Select(parameter => new LabResultParameterResponse(
                parameter.ParameterName,
                parameter.Value,
                parameter.Unit,
                parameter.ReferenceLow,
                parameter.ReferenceHigh,
                parameter.ReferenceText,
                parameter.IsAbnormal,
                parameter.IsCritical,
                parameter.Notes)).ToList());
    }

    private static IReadOnlyList<LabResultParameterSnapshot> DeserializeParameters(string json)
    {
        return JsonSerializer.Deserialize<List<LabResultParameterSnapshot>>(json) ?? [];
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Blue.Lighten4)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(4);
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(4);
    }

    private sealed record LabResultParameterSnapshot(
        string ParameterName,
        string Value,
        string? Unit,
        decimal? ReferenceLow,
        decimal? ReferenceHigh,
        string? ReferenceText,
        bool IsAbnormal,
        bool IsCritical,
        string? Notes,
        decimal? CriticalLow,
        decimal? CriticalHigh);

    private sealed record RecipientProfile(Domain.Identity.ApplicationUser User, string? PhoneNumber);
}
