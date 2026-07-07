using MediatR;

namespace Messenger.Application.Features.Storage.Commands.GenerateUploadUrl;

/// <summary>
/// Command to request a pre-signed URL for uploading a file directly to storage.
/// </summary>
/// <param name="FileName">The desired filename (can include path prefix).</param>
/// <param name="ContentType">The MIME type of the file to be uploaded.</param>
public sealed record GenerateUploadUrlCommand(string FileName, string ContentType)
    : IRequest<GenerateUploadUrlResponse>;

public sealed record GenerateUploadUrlResponse(string UploadUrl, string FileKey);
