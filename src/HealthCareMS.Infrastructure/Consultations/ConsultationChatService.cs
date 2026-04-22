using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Infrastructure.Consultations;

public sealed class ConsultationChatService(
    HealthCareDbContext dbContext,
    IChatFileStorage fileStorage,
    IOptions<ChatFileStorageOptions> storageOptions,
    IConsultationChatNotifier? notifier = null) : IConsultationChatService
{
    private readonly ChatFileStorageOptions storageOptions = storageOptions.Value;

    public async Task<Result<IReadOnlyList<ChatMessageResponse>>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var sessionExists = await dbContext.ConsultationSessions.AnyAsync(x => x.Id == sessionId, cancellationToken);
        if (!sessionExists)
        {
            return Result<IReadOnlyList<ChatMessageResponse>>.Failure(new Error("CHAT_SESSION_NOT_FOUND", "Consultation session was not found."));
        }

        var messages = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ChatMessageResponse>>.Success(messages.Select(Map).ToList());
    }

    public async Task<Result<ChatMessageResponse>> SendMessageAsync(
        Guid sessionId,
        SendChatMessageRequest request,
        Guid? senderUserId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateSessionAndSenderAsync(sessionId, request.ParticipantType, request.SenderDisplayName, cancellationToken);
        if (validation.IsFailure)
        {
            return Result<ChatMessageResponse>.Failure(validation.Error);
        }

        if (string.IsNullOrWhiteSpace(request.MessageText))
        {
            return Result<ChatMessageResponse>.Failure(Error.Validation([
                new ValidationError(nameof(request.MessageText), "MessageText is required.")
            ]));
        }

        var now = DateTimeOffset.UtcNow;
        var participantType = NormalizeParticipant(request.ParticipantType)!;
        var message = new ChatMessage
        {
            SessionId = sessionId,
            SenderUserId = senderUserId,
            SenderType = participantType,
            SenderDisplayName = request.SenderDisplayName.Trim(),
            MessageType = ChatMessageType.Text,
            MessageText = request.MessageText.Trim(),
            SentAt = now
        };

        MarkSenderRead(message, participantType, now);
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = Map(message);
        if (notifier is not null)
        {
            await notifier.NotifyMessageSentAsync(response, cancellationToken);
        }

        return Result<ChatMessageResponse>.Success(response);
    }

    public async Task<Result<ChatMessageResponse>> UploadAttachmentAsync(
        Guid sessionId,
        UploadChatAttachmentRequest request,
        Guid? senderUserId,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateSessionAndSenderAsync(sessionId, request.ParticipantType, request.SenderDisplayName, cancellationToken);
        if (validation.IsFailure)
        {
            return Result<ChatMessageResponse>.Failure(validation.Error);
        }

        if (string.IsNullOrWhiteSpace(request.FileName) || request.Length <= 0)
        {
            return Result<ChatMessageResponse>.Failure(new Error("CHAT_ATTACHMENT_INVALID", "Attachment file is required."));
        }

        if (request.Length > storageOptions.MaxFileSizeBytes)
        {
            return Result<ChatMessageResponse>.Failure(new Error("CHAT_ATTACHMENT_TOO_LARGE", "Attachment file exceeds the configured size limit."));
        }

        var storedFile = await fileStorage.SaveAsync(
            sessionId,
            request.FileName,
            request.ContentType,
            request.Content,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var participantType = NormalizeParticipant(request.ParticipantType)!;
        var message = new ChatMessage
        {
            SessionId = sessionId,
            SenderUserId = senderUserId,
            SenderType = participantType,
            SenderDisplayName = request.SenderDisplayName.Trim(),
            MessageType = ChatMessageType.File,
            MessageText = request.FileName.Trim(),
            AttachmentFileName = storedFile.FileName,
            AttachmentContentType = storedFile.ContentType,
            AttachmentStoragePath = storedFile.StoragePath,
            AttachmentSizeBytes = storedFile.SizeBytes,
            SentAt = now
        };

        MarkSenderRead(message, participantType, now);
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = Map(message);
        if (notifier is not null)
        {
            await notifier.NotifyMessageSentAsync(response, cancellationToken);
        }

        return Result<ChatMessageResponse>.Success(response);
    }

    public async Task<Result<IReadOnlyList<ChatMessageResponse>>> MarkReadAsync(
        Guid sessionId,
        MarkChatMessagesReadRequest request,
        CancellationToken cancellationToken)
    {
        var participantType = NormalizeParticipant(request.ParticipantType);
        if (participantType is null)
        {
            return Result<IReadOnlyList<ChatMessageResponse>>.Failure(new Error("CHAT_PARTICIPANT_INVALID", "ParticipantType must be Patient or Doctor."));
        }

        var messages = await dbContext.ChatMessages
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            var sessionExists = await dbContext.ConsultationSessions.AnyAsync(x => x.Id == sessionId, cancellationToken);
            if (!sessionExists)
            {
                return Result<IReadOnlyList<ChatMessageResponse>>.Failure(new Error("CHAT_SESSION_NOT_FOUND", "Consultation session was not found."));
            }
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var message in messages)
        {
            if (participantType == "Patient")
            {
                message.PatientReadAt ??= now;
            }
            else
            {
                message.DoctorReadAt ??= now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var response = messages.Select(Map).ToList();
        if (notifier is not null)
        {
            await notifier.NotifyReadReceiptsUpdatedAsync(sessionId, response, cancellationToken);
        }

        return Result<IReadOnlyList<ChatMessageResponse>>.Success(response);
    }

    public async Task<Result<ChatAttachmentDownloadResponse>> DownloadAttachmentAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var message = await dbContext.ChatMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == messageId, cancellationToken);

        if (message is null)
        {
            return Result<ChatAttachmentDownloadResponse>.Failure(new Error("CHAT_MESSAGE_NOT_FOUND", "Chat message was not found."));
        }

        if (message.MessageType != ChatMessageType.File
            || string.IsNullOrWhiteSpace(message.AttachmentStoragePath)
            || string.IsNullOrWhiteSpace(message.AttachmentFileName))
        {
            return Result<ChatAttachmentDownloadResponse>.Failure(new Error("CHAT_ATTACHMENT_NOT_FOUND", "Chat attachment was not found."));
        }

        return await fileStorage.OpenReadAsync(
            message.AttachmentStoragePath,
            message.AttachmentFileName,
            message.AttachmentContentType ?? "application/octet-stream",
            cancellationToken);
    }

    private async Task<Result> ValidateSessionAndSenderAsync(
        Guid sessionId,
        string participantType,
        string senderDisplayName,
        CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty)
        {
            return Result.Failure(Error.Validation([new ValidationError(nameof(sessionId), "SessionId is required.")]));
        }

        if (NormalizeParticipant(participantType) is null)
        {
            return Result.Failure(new Error("CHAT_PARTICIPANT_INVALID", "ParticipantType must be Patient or Doctor."));
        }

        if (string.IsNullOrWhiteSpace(senderDisplayName))
        {
            return Result.Failure(Error.Validation([new ValidationError(nameof(senderDisplayName), "SenderDisplayName is required.")]));
        }

        var sessionExists = await dbContext.ConsultationSessions.AnyAsync(x => x.Id == sessionId, cancellationToken);
        return sessionExists
            ? Result.Success()
            : Result.Failure(new Error("CHAT_SESSION_NOT_FOUND", "Consultation session was not found."));
    }

    private static void MarkSenderRead(ChatMessage message, string participantType, DateTimeOffset readAt)
    {
        if (participantType == "Patient")
        {
            message.PatientReadAt = readAt;
        }
        else
        {
            message.DoctorReadAt = readAt;
        }
    }

    private static string? NormalizeParticipant(string participantType)
    {
        if (string.Equals(participantType, "Patient", StringComparison.OrdinalIgnoreCase))
        {
            return "Patient";
        }

        if (string.Equals(participantType, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            return "Doctor";
        }

        return null;
    }

    private static ChatMessageResponse Map(ChatMessage message)
    {
        return new ChatMessageResponse(
            message.Id,
            message.SessionId,
            message.SenderUserId,
            message.SenderType,
            message.SenderDisplayName,
            message.MessageType.ToString(),
            message.MessageText,
            message.AttachmentFileName,
            message.AttachmentContentType,
            message.AttachmentSizeBytes,
            message.MessageType == ChatMessageType.File ? $"/api/v1/consultations/chat/messages/{message.Id}/attachment" : null,
            message.SentAt,
            message.PatientReadAt,
            message.DoctorReadAt);
    }
}
