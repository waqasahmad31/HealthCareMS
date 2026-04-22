using HealthCareMS.Application.Consultations;
using HealthCareMS.Shared.Common;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Infrastructure.Consultations;

public sealed class LocalChatFileStorage(IOptions<ChatFileStorageOptions> options) : IChatFileStorage
{
    private readonly ChatFileStorageOptions options = options.Value;

    public async Task<StoredChatFile> SaveAsync(
        Guid sessionId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var safeFileName = SanitizeFileName(fileName);
        var relativePath = Path.Combine(sessionId.ToString("N"), $"{Guid.NewGuid():N}_{safeFileName}");
        var fullPath = ToFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, cancellationToken);

        return new StoredChatFile(relativePath, safeFileName, NormalizeContentType(contentType), output.Length);
    }

    public Task<Result<ChatAttachmentDownloadResponse>> OpenReadAsync(
        string storagePath,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var fullPath = ToFullPath(storagePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(Result<ChatAttachmentDownloadResponse>.Failure(new Error("CHAT_ATTACHMENT_NOT_FOUND", "Chat attachment was not found.")));
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(Result<ChatAttachmentDownloadResponse>.Success(
            new ChatAttachmentDownloadResponse(stream, fileName, NormalizeContentType(contentType))));
    }

    private string ToFullPath(string relativePath)
    {
        var root = Path.GetFullPath(options.RootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid chat attachment path.");
        }

        return fullPath;
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "Attachment.bin" : name;
    }

    private static string NormalizeContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
    }
}
